using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeamBasedShooter
{
    public class HealthUI : MonoBehaviour
    {
        public TextMeshProUGUI Value;
        public GameObject HitTakenEffect;

        private int _lastHealth;

        private void Awake()
        {
            HitTakenEffect.SetActive(false);
        }

        public void UpdateHealth(Health health)
        {
            int currentHealth = Mathf.CeilToInt(health.CurrentHealth);

            if (currentHealth == _lastHealth)
                return;

            Value.text = currentHealth.ToString();

            if (currentHealth < _lastHealth)
            {
                HitTakenEffect.SetActive(false);
                HitTakenEffect.SetActive(true);
            }

            _lastHealth = currentHealth;
        }
    }
}
