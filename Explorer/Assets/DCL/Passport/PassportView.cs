using DCL.CharacterPreview;
using DCL.Passport.Modals;
using DCL.Passport.Modules;
using DCL.Passport.Modules.Badges;
using DCL.UI;
using MVC;
using UnityEngine;
using UnityEngine.UI;

namespace DCL.Passport
{
    public class PassportView : ViewBase, IView
    {
        [field: SerializeField]
        public Button CloseButton { get; private set; }

        [field: SerializeField]
        public ScrollRect MainScroll { get; private set; }

        [field: SerializeField]
        public Button BackgroundButton { get; private set; }

        [field: SerializeField]
        public Image BackgroundImage { get; private set; }

        [field: SerializeField]
        public CharacterPreviewView CharacterPreviewView { get; private set; }

        [field: SerializeField]
        public UserBasicInfo_PassportModuleView UserBasicInfoModuleView { get; private set; }

        [field: SerializeField]
        public UserDetailedInfo_PassportModuleView UserDetailedInfoModuleView { get; private set; }

        [field: SerializeField]
        public EquippedItems_PassportModuleView EquippedItemsModuleView { get; private set; }

        [field: SerializeField]
        public BadgesOverview_PassportModuleView BadgesOverviewModuleView { get; private set; }

        [field: SerializeField]
        public BadgesDetails_PassportModuleView BadgesDetailsModuleView { get; private set; }

        [field: SerializeField]
        public BadgeInfo_PassportModuleView BadgeInfoModuleView { get; private set; }

        [field: SerializeField]
        public RectTransform MainContainer { get; private set; }

        [field: SerializeField]
        public AddLink_PassportModal AddLinkModal { get; private set; }

        [field: SerializeField]
        public WarningNotificationView ErrorNotification { get; private set; }

        [field: SerializeField]
        public ButtonWithSelectableStateView OverviewSectionButton { get; private set; }

        [field: SerializeField]
        public ButtonWithSelectableStateView BadgesSectionButton { get; private set; }

        [field: SerializeField]
        public GameObject OverviewSectionPanel { get; private set; }

        [field: SerializeField]
        public GameObject BadgesSectionPanel { get; private set; }
    }
}
