using Fusion;
using UnityEngine;
using FusionHelpers;
using System.Collections.Generic;

namespace TeamBasedShooter
{
    public class FragmentPoolManager : NetworkBehaviour
    {
        public static FragmentPoolManager Instance { get; private set; }

        [SerializeField] private BuildingFragment fragmentPrefab;
        [SerializeField] private int initialPoolSize = 10;

        private Queue<BuildingFragment> fragmentPool = new Queue<BuildingFragment>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        public void Initialize()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateFragment();
            }
        }

        private void CreateFragment()
        {
            Vector3 startPosition = new Vector3(1000, 10, 0);

            Runner.Spawn(fragmentPrefab, startPosition, Quaternion.identity, null, (runner, o) =>
            {
                fragmentPool.Enqueue(o.gameObject.GetComponent<BuildingFragment>());
            });
        }

        public BuildingFragment GetFragment(Vector3 position, Quaternion rotation, float force)
        {
            if (fragmentPool.Count == 0)
            {
                CreateFragment();
            }

            BuildingFragment fragment = fragmentPool.Dequeue();
            fragment.Initialize(position, rotation, force);

            return fragment;
        }

        public void ReturnFragment(BuildingFragment fragment)
        {
            fragment.Reset();
            fragmentPool.Enqueue(fragment);
        }
    }

}