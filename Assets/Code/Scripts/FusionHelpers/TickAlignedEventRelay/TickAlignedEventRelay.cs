using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace FusionHelpers
{
	public interface INetworkEvent : INetworkStruct
	{
	}

	public class TickAlignedEventRelay : NetworkBehaviour
	{
		const int MAX_EVENTS = 10;

		const int MAX_EVENT_SIZE = 24;

		private struct EventHeader : INetworkStruct
		{
			public int id { get; set; }
			public int type { get; set; }
			public NetworkId target { get; set; }
		}

		[Networked, Capacity(MAX_EVENTS)] private NetworkArray<EventHeader> _eventHeaders => default;
		[Networked, Capacity(MAX_EVENTS * MAX_EVENT_SIZE)] private NetworkArray<byte> _eventBuffer => default;

		private int _nextEventIndex = 1;
		private int _handledEventIndex;

		private unsafe delegate void ITypeWrapper(int typeIndex, byte* data);
		private List<Type> _registeredTypes = new();
		private List<ITypeWrapper> _listeners = new();

		public void RegisterEventListener<T>(Action<T> listener) where T : unmanaged, INetworkEvent
		{
			int monitoredTypeIndex = _registeredTypes.IndexOf(typeof(T));
			if (monitoredTypeIndex < 0)
			{
				monitoredTypeIndex = _registeredTypes.Count;
				_registeredTypes.Add(typeof(T));
			}

			unsafe
			{
				_listeners.Add((int typeIndex, byte* data) =>
				{
					if (typeIndex == monitoredTypeIndex)
					{
						listener(*(T*)data);
					}
				});
			}
		}

		public void RaiseEventFor<T>(TickAlignedEventRelay target, T evt) where T : unmanaged, INetworkEvent
		{
			unsafe
			{
				Assert.Check(sizeof(T) < MAX_EVENT_SIZE, $"Event of type {typeof(T)} is larger ({sizeof(T)} bytes) than MAX_EVENT_SIZE ({MAX_EVENT_SIZE} bytes)");
			}

			byte[] bytes = SerializeValueType(evt);

			int typeIndex = _registeredTypes.IndexOf(typeof(T));
			target.OnTickAlignedEvent(typeIndex, bytes);

			if (Runner == null || Runner.Topology != Topologies.Shared)
				return;

			if (!target.HasStateAuthority)
			{
				EventHeader head = new();
				head.target = target.Object.Id;
				head.id = _nextEventIndex;
				head.type = typeIndex;
				int index = _nextEventIndex % _eventHeaders.Length;
				_eventHeaders.Set(index, head);
				for (int i = 0; i < bytes.Length; i++)
				{
					_eventBuffer.Set(index * MAX_EVENT_SIZE + i, bytes[i]);
				}
				_nextEventIndex++;
			}
		}

		private unsafe void OnTickAlignedEvent(int typeIndex, byte[] evt)
		{
			fixed (byte* buffer = evt)
			{
				foreach (ITypeWrapper listener in _listeners)
				{
					listener(typeIndex, buffer);
				}
			}
		}

		public override void Render()
		{
			if (HasStateAuthority)
				return;

			if (TryGetSnapshotsBuffers(out var fromBuffer, out _, out _))
			{
				var headersReader = GetArrayReader<EventHeader>(nameof(_eventHeaders));
				var headers = headersReader.Read(fromBuffer);
				var byteReader = GetArrayReader<byte>(nameof(_eventBuffer));
				var bytes = byteReader.Read(fromBuffer);
				int handledId = _handledEventIndex;
				for (int i = 0; i < headers.Length; i++)
				{
					EventHeader head = headers[i];
					if (head.id > _handledEventIndex)
					{
						handledId = Mathf.Max(handledId, head.id);
						if (Runner.TryFindObject(head.target, out NetworkObject no))
						{
							TickAlignedEventRelay behaviour = no.GetComponent<TickAlignedEventRelay>();
							byte[] buffer = new byte[MAX_EVENT_SIZE];
							for (int b = 0; b < buffer.Length; b++)
								buffer[b] = bytes[i * MAX_EVENT_SIZE + b];
							behaviour.OnTickAlignedEvent(head.type, buffer);
						}
					}
				}
				_handledEventIndex = handledId;
			}
		}

		public static unsafe byte[] SerializeValueType<T>(in T value) where T : unmanaged
		{
			byte[] result = new byte[sizeof(T)];
			fixed (byte* dst = result)
				*(T*)dst = value;
			return result;
		}
	}
}
