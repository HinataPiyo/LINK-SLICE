namespace Enemy
{
    using UnityEngine;
    
    public class LongRange : Attack
    {
        [SerializeField] GameObject bulletPrefab;
        [SerializeField] float bulletSpeed = 15f;

        public override void OnAction(IDamageable target)
        {
            Vector3 targetPosition = target.GetPosition();
            Vector3 direction = (targetPosition - transform.position).normalized;
            GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            bullet.Initialize(direction, bulletSpeed, strength);
        }
    }
}