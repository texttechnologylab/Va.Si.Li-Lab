using Avatar = Ubiq.Avatars.Avatar;
using Oculus.Avatar2;

namespace VaSiLi.MetaAvatar
{
    public class MetaAvatar : Avatar
    {

        LocalSampleAvatarEntity localEntity;
        NetworkSampleAvatarEntity networkEntity;
        /*
        public override bool IsLocal
        {
            get
            {
                return isLocal;
            }
            set
            {
                isLocal = value;
                SetupAvatarScript();
            }
        }*/


        public OvrAvatarEntity GetActiveAvatarScript()
        {
            if (localEntity == null || networkEntity == null)
                InitEntities();

            if (IsLocal)
            {
                localEntity.enabled = true;
                networkEntity.enabled = false;
                return localEntity;
            }
            else
            {
                localEntity.enabled = false;
                networkEntity.enabled = true;
                return networkEntity;
            }
        }

        // Can be solved more nicely in start().
        // But start() is not overwriteable by default.
        // Therefore I leave it like this for the time being.
        private void InitEntities()
        {
            localEntity = gameObject.GetComponentInChildren<LocalSampleAvatarEntity>();
            networkEntity = gameObject.GetComponentInChildren<NetworkSampleAvatarEntity>();
        }

        private void SetupAvatarScript()
        {
            if (localEntity == null || networkEntity == null)
                InitEntities();
            if (IsLocal)
            {
                localEntity.enabled = true;
                networkEntity.enabled = false;
            }
            else
            {
                localEntity.enabled = false;
                networkEntity.enabled = true;
            }


        }
    }
}
