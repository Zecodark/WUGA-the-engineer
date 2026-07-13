using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace HPhysic
{
    public class PhysicCable : MonoBehaviour
    {
        [Header("Look")]
        [SerializeField, Min(1)] private int numberOfPoints = 3;
        [SerializeField, Min(0.01f)] private float space = 0.3f;
        [SerializeField, Min(0.01f)] private float size = 0.3f;

        [Header("Cable Visual")]
        [SerializeField] private LineRenderer cableRenderer;
        [SerializeField, Min(0.005f)] private float cableRadius = 0.06f;
        [SerializeField] private Color cableColor = Color.red;
        [SerializeField, Min(0)] private int cableCornerVertices = 8;
        [SerializeField, Min(0)] private int cableCapVertices = 8;
        [SerializeField] private bool hideConnectorMeshes = true;
        [SerializeField] private bool hidePointMeshes = true;

        [Header("Bahaviour")]
        [SerializeField, Min(1f)] private float springForce = 45f;
        [SerializeField, Min(0f)] private float springDamper = 8f;
        [SerializeField, Min(1f)] private float segmentMaxDistanceMultiplier = 1.2f;
        [SerializeField, Min(1f)] private float carryReachMultiplier = 1.25f;
        [SerializeField, Min(1f)] private float brakeLengthMultiplier = 2f;
        [SerializeField, Min(0.1f)] private float minBrakeTime = 1f;
        [SerializeField] private bool keepRetractedAtStart = false;
        [SerializeField, Min(0.05f)] private float retractedReleaseDistance = 0.9f;

        [Header("Tension & Collision")]
        [SerializeField] private bool straightenWhenConnected = true;
        [SerializeField, Min(1f)] private float connectedStraightenSpeed = 18f;
        [SerializeField] private bool constrainAgainstObstacles = true;
        [SerializeField] private LayerMask obstacleMask = ~0;
        [SerializeField, Min(0.01f)] private float segmentCollisionRadius = 0.09f;
        [SerializeField, Min(0.01f)] private float obstacleClearance = 0.08f;
        [SerializeField, Range(1, 5)] private int obstacleSolveIterations = 2;
        [SerializeField] private Transform[] connectedRoutePoints;

        private float brakeLength;
        private float timeToBrake = 1f;

        [Header("Object to set")]
        [SerializeField, Required] private GameObject start;
        [SerializeField, Required] private GameObject end;
        [SerializeField, Required] private GameObject connector0;
        [SerializeField, Required] private GameObject point0;

        private List<Transform> points;
        private List<Transform> connectors;

        private const string cloneText = "Part";

        private Connector startConnector;
        private Connector endConnector;
        private bool retractionReleased;
        private Vector3 retractedEndLocalPosition;
        private Quaternion retractedEndLocalRotation;

        [Button("Reset points")]
        private void UpdatePoints()
        {
            if (!start || !end || !point0 || !connector0)
            {
                Debug.LogWarning("Can't update because one of objects to set is null!");
                return;
            }

            // delete old
            int length = transform.childCount;
            for (int i = 0; i < length; i++)
                if (transform.GetChild(i).name.StartsWith(cloneText))
                {
                    DestroyImmediate(transform.GetChild(i).gameObject);
                    length--;
                    i--;
                }

            // set new
            Vector3 lastPos = start.transform.position;
            Rigidbody lasBody = start.GetComponent<Rigidbody>();
            for (int i = 0; i < numberOfPoints; i++)
            {
                GameObject cConnector = i == 0 ? connector0 : CreateNewCon(i);
                GameObject cPoint = i == 0 ? point0 : CreateNewPoint(i);

                Vector3 newPos = CountNewPointPos(lastPos);
                cPoint.transform.position = newPos;
                cPoint.transform.localScale = Vector3.one * size;
                cPoint.transform.rotation = transform.rotation;

                SetSpirng(cPoint.GetComponent<SpringJoint>(), lasBody);

                lasBody = cPoint.GetComponent<Rigidbody>();

                cConnector.transform.position = CountConPos(lastPos, newPos);
                cConnector.transform.localScale = CountSizeOfCon(lastPos, newPos);
                cConnector.transform.rotation = CountRoationOfCon(lastPos, newPos);
                lastPos = newPos;
            }

            Vector3 endPos = CountNewPointPos(lastPos);
            end.transform.position = endPos;
            SetSpirng(lasBody.gameObject.AddComponent<SpringJoint>(), end.GetComponent<Rigidbody>());

            GameObject endConnector = CreateNewCon(numberOfPoints);
            endConnector.transform.position = CountConPos(lastPos, endPos);
            endConnector.transform.rotation = CountRoationOfCon(lastPos, endPos);

            RefreshCableVisual();

            Vector3 CountNewPointPos(Vector3 lastPos) => lastPos + transform.forward * space;
        }

        [Button("Add point")]
        private void AddPoint()
        {
            Transform lastprevPoint = GetPoint(numberOfPoints - 1);
            if (lastprevPoint == null)
            {
                Debug.LogWarning("Dont found point number " + (numberOfPoints - 1));
                return;
            }

            Rigidbody endRB = end.GetComponent<Rigidbody>();
            foreach (var spring in lastprevPoint.GetComponents<SpringJoint>())
                if (spring.connectedBody == endRB)
                    DestroyImmediate(spring);

            GameObject cPoint = CreateNewPoint(numberOfPoints);
            GameObject cConnector = CreateNewCon(numberOfPoints + 1);

            cPoint.transform.position = end.transform.position;
            cPoint.transform.rotation = end.transform.rotation;
            cPoint.transform.localScale = Vector3.one * size;

            SetSpirng(cPoint.GetComponent<SpringJoint>(), lastprevPoint.GetComponent<Rigidbody>());
            SetSpirng(cPoint.AddComponent<SpringJoint>(), endRB);

            // fix end
            end.transform.position += end.transform.forward * space;

            cConnector.transform.position = CountConPos(cPoint.transform.position, end.transform.position);
            cConnector.transform.localScale = CountSizeOfCon(cPoint.transform.position, end.transform.position);
            cConnector.transform.rotation = CountRoationOfCon(cPoint.transform.position, end.transform.position);

            numberOfPoints++;
            RefreshCableVisual();
        }

        [Button("Remove point")]
        private void RemovePoint()
        {
            if (numberOfPoints < 2)
            {
                Debug.LogWarning("Cable can't be shorter then 1");
                return;
            }

            Transform lastprevPoint = GetPoint(numberOfPoints - 1);
            if (lastprevPoint == null)
            {
                Debug.LogWarning("Dont found point number " + (numberOfPoints - 1));
                return;
            }

            Transform lastprevCon = GetConnector(numberOfPoints);
            if (lastprevCon == null)
            {
                Debug.LogWarning("Dont found connector number " + (numberOfPoints));
                return;
            }

            Transform lastlastprevPoint = GetPoint(numberOfPoints - 2);
            if (lastlastprevPoint == null)
            {
                Debug.LogWarning("Dont found point number " + (numberOfPoints - 2));
                return;
            }


            Rigidbody endRB = end.GetComponent<Rigidbody>();
            SetSpirng(lastlastprevPoint.gameObject.AddComponent<SpringJoint>(), endRB);

            end.transform.position = lastprevPoint.position;
            end.transform.rotation = lastprevPoint.rotation;

            DestroyImmediate(lastprevPoint.gameObject);
            DestroyImmediate(lastprevCon.gameObject);

            numberOfPoints--;
            RefreshCableVisual();
        }

        [Button("Refresh visual")]
        private void RefreshCableVisual()
        {
            BuildCableLists();
            SetupCableVisual();
            UpdateCableVisual();
        }


        private void Start()
        {
            startConnector = start.GetComponent<Connector>();
            endConnector = end.GetComponent<Connector>();
            CacheRetractedEndPose();

            ConfigureExistingSprings();

            brakeLength = MaxReachLength * brakeLengthMultiplier + 2f;

            BuildCableLists();
            SetupCableVisual();
            UpdateCableVisual();
        }

        private void BuildCableLists()
        {
            points = new List<Transform>();
            connectors = new List<Transform>();

            AddPointIfValid(start ? start.transform : null);
            AddPointIfValid(point0 ? point0.transform : null);
            AddConnectorIfValid(connector0 ? connector0.transform : null);

            for (int i = 1; i < numberOfPoints; i++)
            {
                Transform conn = GetConnector(i);
                if (conn == null)
                    Debug.LogWarning("Dont found connector number " + i);
                else
                    AddConnectorIfValid(conn);

                Transform point = GetPoint(i);
                if (point == null)
                    Debug.LogWarning("Dont found point number " + i);
                else
                    AddPointIfValid(point);
            }

            Transform endConn = GetConnector(numberOfPoints);
            if (endConn == null)
                Debug.LogWarning("Dont found connector number " + numberOfPoints);
            else
                AddConnectorIfValid(endConn);

            AddPointIfValid(end ? end.transform : null);
        }

        private void Update()
        {
            float cableLength = 0f;
            bool isConnected = startConnector.IsConnected || endConnector.IsConnected;

            if (points == null || connectors == null || points.Count < 2)
                return;

            KeepRetractedPointsNearStart();

            if (straightenWhenConnected && IsConnectedToExternalSocket())
                StraightenConnectedCable();

            if (constrainAgainstObstacles)
                ResolveObstacleCollisions();

            int numOfParts = Mathf.Min(connectors.Count, points.Count - 1);
            Transform lastPoint = points[0];
            for (int i = 0; i < numOfParts; i++)
            {
                Transform nextPoint = points[i + 1];
                Transform connector = connectors[i].transform;
                if (lastPoint == null || nextPoint == null || connector == null)
                    continue;

                connector.position = CountConPos(lastPoint.position, nextPoint.position);
                if (lastPoint.position == nextPoint.position || nextPoint.position == connector.position)
                {
                    connector.localScale = Vector3.zero;
                }
                else
                {
                    connector.rotation = Quaternion.LookRotation(nextPoint.position - connector.position);
                    connector.localScale = CountSizeOfCon(lastPoint.position, nextPoint.position);
                }

                if (isConnected)
                    cableLength += (lastPoint.position - nextPoint.position).magnitude;

                lastPoint = nextPoint;
            }

            UpdateCableVisual();

            if (isConnected)
            {
                if (cableLength > brakeLength)
                {
                    timeToBrake -= Time.deltaTime;
                    if (timeToBrake < 0f)
                    {
                        startConnector.Disconnect();
                        endConnector.Disconnect();
                        timeToBrake = minBrakeTime;
                    }
                }
                else
                {
                    timeToBrake = minBrakeTime;
                }
            }
        }

        private Vector3 CountConPos(Vector3 start, Vector3 end) => (start + end) / 2f;
        private Vector3 CountSizeOfCon(Vector3 start, Vector3 end) => new Vector3(size, size, (start - end).magnitude / 2f);
        private Quaternion CountRoationOfCon(Vector3 start, Vector3 end) => Quaternion.LookRotation(end - start, Vector3.right);
        private string ConnectorName(int index) => $"{cloneText}_{index}_Conn";
        private string PointName(int index) => $"{cloneText}_{index}_Point";
        private Transform GetConnector(int index) => index > 0 ? transform.Find(ConnectorName(index)) : connector0.transform;
        private Transform GetPoint(int index) => index > 0 ? transform.Find(PointName(index)) : point0.transform;

        private void AddPointIfValid(Transform point)
        {
            if (point != null && !points.Contains(point))
                points.Add(point);
        }

        private void AddConnectorIfValid(Transform connector)
        {
            if (connector != null && !connectors.Contains(connector))
                connectors.Add(connector);
        }


        public void SetSpirng(SpringJoint spring, Rigidbody connectedBody)
        {
            spring.connectedBody = connectedBody;
            spring.spring = springForce;
            spring.damper = springDamper;
            spring.autoConfigureConnectedAnchor = false;
            spring.anchor = Vector3.zero;
            spring.connectedAnchor = Vector3.zero;
            spring.minDistance = 0f;
            spring.maxDistance = space * segmentMaxDistanceMultiplier;
            spring.enablePreprocessing = false;
        }

        private void ConfigureExistingSprings()
        {
            Rigidbody previousBody = start.GetComponent<Rigidbody>();
            ConfigureCableBody(previousBody);

            for (int i = 0; i < numberOfPoints; i++)
            {
                Transform point = GetPoint(i);
                if (point == null)
                    continue;

                ConfigureCableBody(point.GetComponent<Rigidbody>());

                SpringJoint spring = FindSpringConnectedTo(point, previousBody);
                if (spring != null)
                    SetSpirng(spring, previousBody);

                previousBody = point.GetComponent<Rigidbody>();
            }

            Transform lastPoint = GetPoint(numberOfPoints - 1);
            if (lastPoint == null)
                return;

            Rigidbody endBody = end.GetComponent<Rigidbody>();
            ConfigureCableBody(endBody);

            SpringJoint endSpring = FindSpringConnectedTo(lastPoint, endBody);
            if (endSpring != null)
                SetSpirng(endSpring, endBody);
        }

        private void ConfigureCableBody(Rigidbody body)
        {
            if (body == null)
                return;

            body.collisionDetectionMode = body.isKinematic
                ? CollisionDetectionMode.ContinuousSpeculative
                : CollisionDetectionMode.ContinuousDynamic;
            body.solverIterations = Mathf.Max(body.solverIterations, 12);
            body.solverVelocityIterations = Mathf.Max(body.solverVelocityIterations, 4);
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private SpringJoint FindSpringConnectedTo(Transform target, Rigidbody connectedBody)
        {
            foreach (SpringJoint spring in target.GetComponents<SpringJoint>())
                if (spring.connectedBody == connectedBody)
                    return spring;

            return null;
        }

        private void SetupCableVisual()
        {
            if (cableRenderer == null)
            {
                GameObject visualObject = new GameObject("CableVisual");
                visualObject.transform.SetParent(transform);
                visualObject.transform.localPosition = Vector3.zero;
                visualObject.transform.localRotation = Quaternion.identity;
                visualObject.transform.localScale = Vector3.one;
                cableRenderer = visualObject.AddComponent<LineRenderer>();
            }

            cableRenderer.useWorldSpace = true;
            cableRenderer.startWidth = cableRadius * 2f;
            cableRenderer.endWidth = cableRadius * 2f;
            cableRenderer.startColor = cableColor;
            cableRenderer.endColor = cableColor;
            cableRenderer.numCornerVertices = cableCornerVertices;
            cableRenderer.numCapVertices = cableCapVertices;

            if (cableRenderer.sharedMaterial == null)
                cableRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

            SetRenderersVisible(connectors, !hideConnectorMeshes);
            SetRenderersVisible(points, !hidePointMeshes, start.transform, end.transform);
        }

        private void UpdateCableVisual()
        {
            if (cableRenderer == null || points == null || points.Count == 0)
                return;

            cableRenderer.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] == null)
                    continue;

                cableRenderer.SetPosition(i, points[i].position);
            }
        }

        private bool IsConnectedToExternalSocket()
        {
            return (startConnector != null && startConnector.IsConnected) ||
                (endConnector != null && endConnector.IsConnected);
        }

        private void StraightenConnectedCable()
        {
            if (points.Count < 3)
                return;

            Vector3[] route = GetConnectedRoute();
            float maxStep = Mathf.Max(0.01f, connectedStraightenSpeed * Time.deltaTime);

            for (int i = 1; i < points.Count - 1; i++)
            {
                Transform point = points[i];
                if (point == null)
                    continue;

                float t = i / (float)(points.Count - 1);
                Vector3 targetPosition = SampleRoute(route, t);
                Vector3 nextPosition = Vector3.MoveTowards(
                    point.position,
                    targetPosition,
                    maxStep
                );

                SetTransformAndBody(point, nextPosition, point.rotation);
            }
        }

        public void SnapConnectedCableToRoute()
        {
            if (points == null || points.Count < 3)
                return;

            Vector3[] route = GetConnectedRoute();

            for (int i = 1; i < points.Count - 1; i++)
            {
                Transform point = points[i];
                if (point == null)
                    continue;

                float t = i / (float)(points.Count - 1);
                Vector3 targetPosition = SampleRoute(route, t);
                SetTransformAndBody(point, targetPosition, point.rotation);
            }

            if (constrainAgainstObstacles)
                ResolveObstacleCollisions();

            UpdateCableVisual();
        }

        private Vector3[] GetConnectedRoute()
        {
            int routePointCount = 0;
            if (connectedRoutePoints != null)
            {
                foreach (Transform routePoint in connectedRoutePoints)
                    if (routePoint != null)
                        routePointCount++;
            }

            Vector3[] route = new Vector3[routePointCount + 2];
            route[0] = points[0].position;

            int index = 1;
            if (connectedRoutePoints != null)
            {
                foreach (Transform routePoint in connectedRoutePoints)
                {
                    if (routePoint == null)
                        continue;

                    route[index] = routePoint.position;
                    index++;
                }
            }

            route[^1] = points[^1].position;
            return route;
        }

        private Vector3 SampleRoute(Vector3[] route, float t)
        {
            if (route == null || route.Length == 0)
                return transform.position;

            if (route.Length == 1)
                return route[0];

            float totalLength = 0f;
            for (int i = 0; i < route.Length - 1; i++)
                totalLength += Vector3.Distance(route[i], route[i + 1]);

            if (totalLength <= 0.001f)
                return route[0];

            float targetDistance = Mathf.Clamp01(t) * totalLength;
            float walkedDistance = 0f;

            for (int i = 0; i < route.Length - 1; i++)
            {
                float segmentLength = Vector3.Distance(route[i], route[i + 1]);
                if (segmentLength <= 0.001f)
                    continue;

                if (walkedDistance + segmentLength >= targetDistance)
                {
                    float segmentT = (targetDistance - walkedDistance) / segmentLength;
                    return Vector3.Lerp(route[i], route[i + 1], segmentT);
                }

                walkedDistance += segmentLength;
            }

            return route[^1];
        }

        private void ResolveObstacleCollisions()
        {
            for (int iteration = 0; iteration < obstacleSolveIterations; iteration++)
            {
                ResolvePointPenetration();
                ResolveSegmentPenetration();
            }
        }

        private void ResolvePointPenetration()
        {
            for (int i = 1; i < points.Count - 1; i++)
            {
                Transform point = points[i];
                if (point == null)
                    continue;

                Collider pointCollider = point.GetComponent<Collider>();
                if (pointCollider == null)
                    continue;

                Collider[] overlaps = Physics.OverlapSphere(
                    point.position,
                    segmentCollisionRadius,
                    obstacleMask,
                    QueryTriggerInteraction.Ignore
                );

                foreach (Collider obstacle in overlaps)
                {
                    if (ShouldIgnoreObstacle(obstacle))
                        continue;

                    if (Physics.ComputePenetration(
                            pointCollider,
                            point.position,
                            point.rotation,
                            obstacle,
                            obstacle.transform.position,
                            obstacle.transform.rotation,
                            out Vector3 direction,
                            out float distance))
                    {
                        Vector3 correctedPosition =
                            point.position +
                            direction * (distance + obstacleClearance);
                        SetTransformAndBody(point, correctedPosition, point.rotation);
                    }
                }
            }
        }

        private void ResolveSegmentPenetration()
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                Transform first = points[i];
                Transform second = points[i + 1];
                if (first == null || second == null)
                    continue;

                Vector3 direction = second.position - first.position;
                float distance = direction.magnitude;
                if (distance <= 0.001f)
                    continue;

                RaycastHit[] hits = Physics.SphereCastAll(
                    first.position,
                    segmentCollisionRadius,
                    direction.normalized,
                    distance,
                    obstacleMask,
                    QueryTriggerInteraction.Ignore
                );

                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider == null || ShouldIgnoreObstacle(hit.collider))
                        continue;

                    Transform movablePoint = i + 1 < points.Count - 1
                        ? second
                        : i > 0
                            ? first
                            : null;

                    if (movablePoint == null)
                        continue;

                    Vector3 correctedPosition =
                        hit.point +
                        hit.normal * (segmentCollisionRadius + obstacleClearance);
                    SetTransformAndBody(
                        movablePoint,
                        correctedPosition,
                        movablePoint.rotation
                    );
                    break;
                }
            }
        }

        private bool ShouldIgnoreObstacle(Collider obstacle)
        {
            if (obstacle == null || obstacle.transform.IsChildOf(transform))
                return true;

            return obstacle.GetComponentInParent<Connector>() != null ||
                obstacle.GetComponentInParent<CableGrabInteraction>() != null;
        }

        private void SetRenderersVisible(
            IReadOnlyList<Transform> targets,
            bool visible,
            params Transform[] exceptions)
        {
            if (targets == null)
                return;

            foreach (Transform target in targets)
            {
                if (target == null || IsException(target, exceptions))
                    continue;

                foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = visible;
            }
        }

        private bool IsException(Transform target, Transform[] exceptions)
        {
            if (exceptions == null)
                return false;

            foreach (Transform exception in exceptions)
                if (target == exception)
                    return true;

            return false;
        }

        private void KeepRetractedPointsNearStart()
        {
            if (!keepRetractedAtStart || retractionReleased || start == null || end == null || points == null || points.Count < 3)
                return;

            Vector3 startPosition = start.transform.position;
            Vector3 endPosition = start.transform.TransformPoint(retractedEndLocalPosition);
            Quaternion endRotation = start.transform.rotation * retractedEndLocalRotation;
            float releaseDistance = Mathf.Max(retractedReleaseDistance, space);

            if ((endPosition - startPosition).sqrMagnitude > releaseDistance * releaseDistance)
                return;

            SetTransformAndBody(end.transform, endPosition, endRotation);

            for (int i = 1; i < points.Count - 1; i++)
            {
                Transform point = points[i];
                if (point == null)
                    continue;

                float t = i / (float)(points.Count - 1);
                Vector3 retractedPosition = Vector3.Lerp(startPosition, endPosition, t);
                SetTransformAndBody(point, retractedPosition, point.rotation);
            }
        }

        private void CacheRetractedEndPose()
        {
            if (!keepRetractedAtStart || start == null || end == null)
                return;

            retractionReleased = false;
            retractedEndLocalPosition = start.transform.InverseTransformPoint(end.transform.position);
            retractedEndLocalRotation = Quaternion.Inverse(start.transform.rotation) * end.transform.rotation;
        }

        private void SetTransformAndBody(Transform target, Vector3 position, Quaternion rotation)
        {
            if (target == null)
                return;

            target.SetPositionAndRotation(position, rotation);

            if (target.TryGetComponent(out Rigidbody body))
            {
                body.position = position;
                body.rotation = rotation;
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
            }
        }

        public void ReleaseRetraction(Transform heldPoint)
        {
            if (!keepRetractedAtStart || retractionReleased)
                return;

            if (heldPoint == end.transform || heldPoint == start.transform)
                retractionReleased = true;
        }

        public Vector3 ClampHeldPosition(Transform heldPoint, Vector3 desiredPosition)
        {
            Transform anchor = heldPoint == end.transform ? start.transform : end.transform;
            Vector3 fromAnchor = desiredPosition - anchor.position;
            float maxReach = MaxReachLength;

            if (fromAnchor.sqrMagnitude <= maxReach * maxReach)
                return desiredPosition;

            return anchor.position + fromAnchor.normalized * maxReach;
        }

        public float MaxReachLength =>
            space * (numberOfPoints + 1) *
            Mathf.Min(carryReachMultiplier, segmentMaxDistanceMultiplier) * 0.98f;
        private GameObject CreateNewPoint(int index)
        {
            GameObject temp = Instantiate(point0);
            temp.name = PointName(index);
            temp.transform.parent = transform;
            return temp;
        }
        private GameObject CreateNewCon(int index)
        {
            GameObject temp = Instantiate(connector0);
            temp.name = ConnectorName(index);
            temp.transform.parent = transform;
            return temp;
        }


        public Connector StartConnector => startConnector;
        public Connector EndConnector => endConnector;
        public IReadOnlyList<Transform> Points => points;
    }
}
