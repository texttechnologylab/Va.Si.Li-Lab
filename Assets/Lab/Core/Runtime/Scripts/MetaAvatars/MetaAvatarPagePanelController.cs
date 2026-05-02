using System.Collections.Generic;
using UnityEngine;
using Ubiq.Avatars;
using Ubiq.Messaging;
using Ubiq.Samples.Social;
using VaSiLi.MetaAvatar;


namespace VaSiLi.MetaAvatar
{
    [RequireComponent(typeof(PagePanel))]
    public class MetaAvatarPagePanelController : MonoBehaviour
    {
        public AvatarTextureCatalogue catalogue;
        public int entriesPerPage = 3;
        private int maxPages;

        public GameObject controlTemplate;
        public Transform controlsRoot;

        private List<AvatarPagePanelControl> controls = new List<AvatarPagePanelControl>();

        private PagePanel pagePanel;
        private NetworkScene networkScene;
        private ulong[] avatarIds = new ulong[] { 25114505871472498, 25076688645296872, 31996848129930621, 25729338716652792 };
        
        // Shuffled indices for randomized display order
        private List<int> shuffledIndices;

        private void Awake()
        {
            pagePanel = GetComponent<PagePanel>();
            maxPages = (catalogue.Count + entriesPerPage - 1) / entriesPerPage;
            
            // Build and shuffle index list for random order each session
            shuffledIndices = new List<int>(catalogue.Count);
            for (int i = 0; i < catalogue.Count; i++)
                shuffledIndices.Add(i);
            
            // Fisher-Yates shuffle
            for (int i = shuffledIndices.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int temp = shuffledIndices[i];
                shuffledIndices[i] = shuffledIndices[j];
                shuffledIndices[j] = temp;
            }
        }

        private void OnEnable()
        {
            UpdateOptions();
            pagePanel.onPageChanged.AddListener(PagePanel_OnPageChanged);
        }

        private void OnDisable()
        {
            if (pagePanel)
            {
                pagePanel.onPageChanged.RemoveListener(PagePanel_OnPageChanged);
            }
        }

        private AvatarPagePanelControl InstantiateControl()
        {
            var go = GameObject.Instantiate(controlTemplate, controlsRoot);
            go.SetActive(true);
            return go.GetComponent<AvatarPagePanelControl>();
        }

        private void PagePanel_OnPageChanged(int page, int pageCount)
        {
            UpdateOptions();
        }

        private void UpdateOptions()
        {
            //var optionCount = Mathf.Min(entriesPerPage * maxPages, presetAvatarCount);
            pagePanel.SetPageCount(maxPages);

            int controlI = 0;
            for (int optionI = 0; optionI < catalogue.Count; controlI++, optionI++)
            {
                if (controls.Count <= controlI)
                {
                    controls.Add(InstantiateControl());
                }

                controls[controlI].Bind(SetTexture, catalogue.Get(shuffledIndices[optionI]));
            }

            while (controls.Count > controlI)
            {
                Destroy(controls[controlI].gameObject);
                controls.RemoveAt(controlI);
            }

            var startOptionI = pagePanel.page * entriesPerPage;
            var endOptionI = pagePanel.page * entriesPerPage + entriesPerPage - 1;

            for (int i = 0; i < controls.Count; i++)
            {
                controls[i].gameObject.SetActive(i >= startOptionI && i <= endOptionI);
            }
        }

        private void SetTexture(Texture2D texture)
        {
            if (!networkScene)
            {
                networkScene = NetworkScene.Find(this);
                if (!networkScene)
                {
                    return;
                }
            }

            var avatar = networkScene.GetComponentInChildren<AvatarManager>().LocalAvatar;
            string textureIdx = catalogue.Get(texture);
            int textureIdxInt = int.Parse(textureIdx);
            if (textureIdxInt == catalogue.Count - 1)
            {
                //TODO!!
                //AvatarEditorDeeplink.LaunchAvatarEditor();
                //textureIdxInt = -1;
            }


            if (avatar.GetType() == typeof(MetaAvatar))
            {
                //I hate this one ....
                LocalSampleAvatarEntity mAvatar = (LocalSampleAvatarEntity)((MetaAvatar)avatar).GetActiveAvatarScript();
                mAvatar.UpdateTexture(avatarIds[textureIdxInt % avatarIds.Length]);
                //mAvatar.LoadRemoteUserCdnAvatar();
            }
            else
            {
                Debug.LogError("This Page Controller needs MetaAvatars!");
            }

        }
    }
}
