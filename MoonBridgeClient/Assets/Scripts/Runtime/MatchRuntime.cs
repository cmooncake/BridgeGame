using MoonBridge.Domain;
using MoonBridge.Game;
using MoonBridge.Game.Authoritative;
using MoonBridge.Presentation;
using UnityEngine;

namespace MoonBridge.Runtime
{
    [DefaultExecutionOrder(-100)]
    public sealed class MatchRuntime : MonoBehaviour
    {
        public static MatchRuntime Instance { get; private set; }

        [SerializeField] private int seed = 36;

        private Table table;
        private ActionRuntime actions;
        private IAuthoritativeSource source;
        private MatchStateMachine stateMachine;
        private PresentationRuntime presentation;
        private bool initialized;

        public Table Table
        {
            get { return table; }
        }

        public ActionRuntime Actions
        {
            get { return actions; }
        }

        public MatchStateMachine StateMachine
        {
            get { return stateMachine; }
        }

        public PresentationRuntime Presentation
        {
            get { return presentation; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        public static MatchRuntime Ensure()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var existing = FindFirstObjectByType<MatchRuntime>();
            if (existing != null)
            {
                existing.Initialize();
                return existing;
            }

            var root = new GameObject(nameof(MatchRuntime));
            return root.AddComponent<MatchRuntime>();
        }

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            actions.DealHands.Emit(seed);
        }

        private void Update()
        {
            if (stateMachine != null)
            {
                stateMachine.Update();
            }
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            if (presentation != null)
            {
                presentation.CancelAll();
            }

            if (stateMachine != null)
            {
                stateMachine.Director.CancelCurrentAndAnimations();
                stateMachine.Unbind();
            }

            Instance = null;
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            initialized = true;
            Instance = this;
            presentation = gameObject.GetComponent<PresentationRuntime>();
            if (presentation == null)
            {
                presentation = gameObject.AddComponent<PresentationRuntime>();
            }

            table = new Table();
            actions = new ActionRuntime();
            source = new LocalAuthoritativeSource(table);
            var seatIntents = new SeatIntentRouter()
                .Bind(Seat.West, new AutoPlayIntentSource())
                .Bind(Seat.North, new AutoPlayIntentSource())
                .Bind(Seat.East, new AutoPlayIntentSource());
            stateMachine = new MatchStateMachine(table, source, actions, seatIntents);
            stateMachine.Bind();
        }
    }
}
