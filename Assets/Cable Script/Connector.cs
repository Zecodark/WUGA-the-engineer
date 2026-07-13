using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace HPhysic
{
    [RequireComponent(typeof(Rigidbody))]
    public class Connector : MonoBehaviour
    {
        public enum ConType { Male, Female }
        public enum CableColor { White, Red, Green, Yellow, Blue, Cyan, Magenta }

        [field: Header("Settings")]

        [field: SerializeField] public ConType ConnectionType { get; private set; } = ConType.Male;
        [field: SerializeField, OnValueChanged(nameof(UpdateConnectorColor))] public CableColor ConnectionColor { get; private set; } = CableColor.White;

        [SerializeField] private bool makeConnectionKinematic = false;
        private bool _wasConnectionKinematic;

        [SerializeField] private bool hideInteractableWhenIsConnected = false;
        [SerializeField] private bool allowConnectDifrentCollor = false;
        [SerializeField, Min(0f)] private float connectedInsertDepth = 0.18f;
        [SerializeField] private Vector3 connectedOutLocalAxis = Vector3.right;
        [SerializeField] private bool preserveIncomingRotationOnConnect = true;

        [field: SerializeField] public Connector ConnectedTo { get; private set; }


        [Header("Object to set")]
        [SerializeField, Required] private Transform connectionPoint;
        [SerializeField] private MeshRenderer collorRenderer;
        [SerializeField] private ParticleSystem correctSparksParticle;
        [SerializeField] private ParticleSystem sparksParticle;


        private FixedJoint _fixedJoint;
        private readonly List<ColliderPair> _ignoredConnectionCollisions = new();
        private bool _isConnectionLocked;
        public Rigidbody Rigidbody { get; private set; }

        public Vector3 ConnectionPosition => connectionPoint ? connectionPoint.position : transform.position;
        public Quaternion ConnectionRotation => connectionPoint ? connectionPoint.rotation : transform.rotation;
        public Quaternion RotationOffset => connectionPoint ? connectionPoint.localRotation : Quaternion.Euler(Vector3.zero);
        public Vector3 ConnectedOutOffset
        {
            get
            {
                Vector3 localAxis = connectedOutLocalAxis.sqrMagnitude > 0.000001f
                    ? connectedOutLocalAxis.normalized
                    : Vector3.right;

                return connectionPoint
                    ? connectionPoint.TransformDirection(localAxis)
                    : transform.TransformDirection(localAxis);
            }
        }
        private Vector3 ConnectionSnapPosition => ConnectionPosition - ConnectedOutOffset * connectedInsertDepth;
        private Quaternion LocalConnectionRotation => connectionPoint ? connectionPoint.localRotation : Quaternion.identity;
        private Vector3 LocalConnectionDirection
        {
            get
            {
                if (connectionPoint == null || connectionPoint.localPosition.sqrMagnitude < 0.000001f)
                    return Vector3.right;

                return connectionPoint.localPosition.normalized;
            }
        }

        public bool IsConnected => ConnectedTo != null;
        public bool IsConnectedRight => IsConnected && ConnectionColor == ConnectedTo.ConnectionColor;
        public bool IsConnectionLocked => _isConnectionLocked ||
            (ConnectedTo != null && ConnectedTo._isConnectionLocked);



        private void Awake()
        {
            Rigidbody = gameObject.GetComponent<Rigidbody>();
            ResolveConnectionPoint();
        }

        private void OnValidate()
        {
            ResolveConnectionPoint();
        }

        private void Start()
        {
            UpdateConnectorColor();

            if (ConnectedTo != null)
            {
                Connector t = ConnectedTo;
                ConnectedTo = null;
                Connect(t);
            }
        }

        private void OnDisable() => Disconnect();

        public void SetAsConnectedTo(Connector secondConnector)
        {
            ConnectedTo = secondConnector;
            _wasConnectionKinematic = secondConnector.Rigidbody.isKinematic;
            UpdateInteractableWhenIsConnected();
        }
        public void Connect(Connector secondConnector)
        {
            if (secondConnector == null)
            {
                Debug.LogWarning("Attempt to connect null");
                return;
            }

            if (IsConnected)
                Disconnect(secondConnector);

            Quaternion targetRotation = preserveIncomingRotationOnConnect
                ? secondConnector.transform.rotation
                : GetAlignedConnectionRotation(secondConnector);

            secondConnector.transform.rotation = targetRotation;

            Vector3 targetPosition = ConnectionSnapPosition - (secondConnector.ConnectionPosition - secondConnector.transform.position);
            secondConnector.transform.position = targetPosition;

            if (secondConnector.Rigidbody != null)
            {
                secondConnector.Rigidbody.position = targetPosition;
                secondConnector.Rigidbody.rotation = targetRotation;
                secondConnector.Rigidbody.linearVelocity = Vector3.zero;
                secondConnector.Rigidbody.angularVelocity = Vector3.zero;
            }

            IgnoreConnectionCollisions(secondConnector, true);
            Physics.SyncTransforms();

            _fixedJoint = gameObject.AddComponent<FixedJoint>();
            _fixedJoint.connectedBody = secondConnector.Rigidbody;

            secondConnector.SetAsConnectedTo(this);
            _wasConnectionKinematic = secondConnector.Rigidbody.isKinematic;
            if (makeConnectionKinematic)
                secondConnector.Rigidbody.isKinematic = true;
            ConnectedTo = secondConnector;

            PlayConnectionFeedback();

            // disable outline on select
            UpdateInteractableWhenIsConnected();
        }

        private Quaternion GetAlignedConnectionRotation(Connector secondConnector)
        {
            Quaternion targetRotation = ConnectionRotation * Quaternion.Inverse(secondConnector.LocalConnectionRotation);
            Vector3 desiredInsertionDirection = -ConnectedOutOffset.normalized;
            Vector3 currentInsertionDirection = targetRotation * secondConnector.LocalConnectionDirection;

            if (desiredInsertionDirection.sqrMagnitude > 0.000001f &&
                currentInsertionDirection.sqrMagnitude > 0.000001f)
            {
                targetRotation =
                    Quaternion.FromToRotation(currentInsertionDirection, desiredInsertionDirection) *
                    targetRotation;
            }

            return targetRotation;
        }
        public void Disconnect(Connector onlyThis = null)
        {
            if (IsConnectionLocked)
                return;

            if (ConnectedTo == null || onlyThis != null && onlyThis != ConnectedTo)
                return;

            IgnoreConnectionCollisions(ConnectedTo, false);
            Destroy(_fixedJoint);

            // important to dont make recusrion
            Connector toDisconect = ConnectedTo;
            ConnectedTo = null;
            if (makeConnectionKinematic)
                toDisconect.Rigidbody.isKinematic = _wasConnectionKinematic;
            toDisconect.Disconnect(this);

            // sparks on incorrect connection
            StopSpark(sparksParticle);
            StopSpark(correctSparksParticle);

            if (incorrectSparksC != null)
            {
                StopCoroutine(incorrectSparksC);
                incorrectSparksC = null;
            }

            // enable outline on select
            UpdateInteractableWhenIsConnected();
        }

        public void LockCurrentConnection()
        {
            if (!IsConnectedRight || ConnectedTo == null)
                return;

            if (IsConnectionLocked)
                return;

            _isConnectionLocked = true;
            ConnectedTo._isConnectionLocked = true;

            LockBody(Rigidbody);
            LockBody(ConnectedTo.Rigidbody);
        }

        private static void LockBody(Rigidbody body)
        {
            if (body == null)
                return;

            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.useGravity = false;
            body.isKinematic = true;
        }

        private void UpdateInteractableWhenIsConnected()
        {
            if (hideInteractableWhenIsConnected)
            {
                if (TryGetComponent(out Collider collider))
                    collider.enabled = !IsConnected;
            }
        }


        private IEnumerator incorrectSparksC;
        private void PlayConnectionFeedback()
        {
            if (!IsConnected)
                return;

            if (IsConnectedRight)
            {
                PlaySpark(correctSparksParticle);
                return;
            }

            if (incorrectSparksC == null && sparksParticle)
            {
                incorrectSparksC = IncorrectSparks();
                StartCoroutine(incorrectSparksC);
            }
        }

        private IEnumerator IncorrectSparks()
        {
            while (incorrectSparksC != null && sparksParticle && IsConnected && !IsConnectedRight)
            {
                PlaySpark(sparksParticle);

                yield return new WaitForSeconds(Random.Range(0.6f, 0.8f));
            }
            incorrectSparksC = null;
        }

        private void PlaySpark(ParticleSystem particle)
        {
            if (particle == null)
                return;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(true);
        }

        private void StopSpark(ParticleSystem particle)
        {
            if (particle == null)
                return;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Clear(true);
        }

        private void UpdateConnectorColor()
        {
            if (collorRenderer == null)
                return;

            Color color = MaterialColor(ConnectionColor);
            MaterialPropertyBlock probs = new();
            collorRenderer.GetPropertyBlock(probs);
            probs.SetColor("_Color", color);
            collorRenderer.SetPropertyBlock(probs);
        }

        private void ResolveConnectionPoint()
        {
            if (connectionPoint != null && connectionPoint.IsChildOf(transform))
                return;

            Transform ownConnectionPoint = FindChildRecursive(transform, "ConnectionPoint");
            if (ownConnectionPoint != null)
                connectionPoint = ownConnectionPoint;
            else
                connectionPoint = null;
        }

        private Transform FindChildRecursive(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;

                Transform found = FindChildRecursive(child, childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private Color MaterialColor(CableColor cableColor) => cableColor switch
        {
            CableColor.White => Color.white,
            CableColor.Red => Color.red,
            CableColor.Green => Color.green,
            CableColor.Yellow => Color.yellow,
            CableColor.Blue => Color.blue,
            CableColor.Cyan => Color.cyan,
            CableColor.Magenta => Color.magenta,
            _ => Color.clear
        };


        public bool CanConnect(Connector secondConnector) =>
            this != secondConnector
            && !this.IsConnected && !secondConnector.IsConnected
            && this.ConnectionType != secondConnector.ConnectionType
            && (this.allowConnectDifrentCollor || secondConnector.allowConnectDifrentCollor || this.ConnectionColor == secondConnector.ConnectionColor);

        private void IgnoreConnectionCollisions(Connector secondConnector, bool ignored)
        {
            if (secondConnector == null)
                return;

            if (!ignored)
            {
                foreach (ColliderPair pair in _ignoredConnectionCollisions)
                    if (pair.First != null && pair.Second != null)
                        Physics.IgnoreCollision(pair.First, pair.Second, false);

                _ignoredConnectionCollisions.Clear();
                return;
            }

            IgnoreConnectionCollisions(secondConnector, false);

            Collider[] ownColliders = GetComponentsInChildren<Collider>(true);
            Collider[] secondColliders = secondConnector.GetComponentsInChildren<Collider>(true);

            foreach (Collider ownCollider in ownColliders)
            {
                if (ownCollider == null)
                    continue;

                foreach (Collider secondCollider in secondColliders)
                {
                    if (secondCollider == null || ownCollider == secondCollider)
                        continue;

                    Physics.IgnoreCollision(ownCollider, secondCollider, true);
                    _ignoredConnectionCollisions.Add(new ColliderPair(ownCollider, secondCollider));
                }
            }
        }

        private readonly struct ColliderPair
        {
            public ColliderPair(Collider first, Collider second)
            {
                First = first;
                Second = second;
            }

            public Collider First { get; }
            public Collider Second { get; }
        }
    }
}
