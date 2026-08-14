using UdonSharp;
using UnityEngine;

namespace StargazingHill
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WorldObservatoryButton : UdonSharpBehaviour
    {
        public const int ActionPrevious = 0;
        public const int ActionNext = 1;
        public const int ActionToggleList = 2;
        public const int ActionSelectLocation = 3;
        public const int ActionToggleDebugPanel = 4;

        public WorldObservatorySelector selector;
        public int action;
        public int locationIndex;

        public override void Interact()
        {
            if (selector == null) return;
            if (action == ActionPrevious) selector.SelectPrevious();
            else if (action == ActionNext) selector.SelectNext();
            else if (action == ActionToggleList) selector.ToggleLocationList();
            else if (action == ActionSelectLocation) selector.SelectLocation(locationIndex);
            else if (action == ActionToggleDebugPanel) selector.ToggleDebugPanel();
        }
    }
}
