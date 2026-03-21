namespace Enemy
{
    using UnityEngine;
    
    public class Movement : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 3f;
        [SerializeField] float defaultSwarmFollowDistance = 0.6f;

        Transform swarmFollowTarget;
        float swarmFollowDistance;
        
        public void SetMoveSpeed(float speed) => moveSpeed = speed;

        public void SetupSwarmLeader()
        {
            swarmFollowTarget = null;
            swarmFollowDistance = defaultSwarmFollowDistance;
        }

        public void SetupSwarmFollower(Transform followTarget, float followDistance)
        {
            swarmFollowTarget = followTarget;
            swarmFollowDistance = followDistance;
        }

        public void Move()
        {
            if (swarmFollowTarget != null)
            {
                MoveAsSwarmFollower();
                return;
            }

            MoveStraightToCore();
        }

        void MoveStraightToCore()
        {
            transform.position = Vector2.MoveTowards(transform.position, Vector2.zero, moveSpeed * Time.deltaTime);
            RotateTowards(Vector2.zero - (Vector2)transform.position);
        }

        void MoveAsSwarmFollower()
        {
            Vector2 currentPosition = transform.position;
            Vector2 followPosition = swarmFollowTarget.position;
            Vector2 offsetFromTarget = currentPosition - followPosition;

            if (offsetFromTarget.sqrMagnitude < 0.0001f)
            {
                offsetFromTarget = ((Vector2)transform.position).normalized;
                if (offsetFromTarget.sqrMagnitude < 0.0001f)
                {
                    offsetFromTarget = Vector2.up;
                }
            }

            Vector2 desiredPosition = followPosition + offsetFromTarget.normalized * swarmFollowDistance;
            transform.position = Vector2.MoveTowards(currentPosition, desiredPosition, moveSpeed * Time.deltaTime);
            RotateTowards(followPosition - (Vector2)transform.position);
        }

        void RotateTowards(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
        }
    }
}