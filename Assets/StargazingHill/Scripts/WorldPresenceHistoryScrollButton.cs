using UdonSharp;

namespace StargazingHill
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WorldPresenceHistoryScrollButton : UdonSharpBehaviour
    {
        public WorldPresenceBoard presenceBoard;
        public bool scrollOlder;

        public override void Interact()
        {
            if (presenceBoard == null) return;
            if (scrollOlder) presenceBoard.ScrollHistoryOlder();
            else presenceBoard.ScrollHistoryNewer();
        }
    }
}
