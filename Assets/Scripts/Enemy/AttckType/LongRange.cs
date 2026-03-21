namespace Enemy
{
    using UnityEngine;
    
    public class LongRange : Attack
    {
        [SerializeField] LongRangeEnemyData longRangeData;

        public override void OnAction(IDamageable target)
        {
            Vector3 targetPosition = target.GetPosition();
            Vector3 direction = (targetPosition - transform.position).normalized;
            GameObject bulletObj = Instantiate(longRangeData.bulletPrefab, transform.position, Quaternion.identity);
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            bullet.Initialize(direction, longRangeData.bulletSpeed, longRangeData.strength, longRangeData.rayLayerMask);
        }
    }
}