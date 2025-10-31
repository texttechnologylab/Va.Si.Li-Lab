using System.Collections;

namespace VaSiLi.Tutorial
{
    public abstract class TutorialStep
    {

        protected TutorialManager manager;

        string _descriptionRTF;
        public string DescriptionRTF
        {
            get
            {
                return _descriptionRTF;
            }

            protected set
            {
                _descriptionRTF = value;
            }
        }

        public TutorialStep(TutorialManager manager)
        {
            this.manager = manager;
            init();
        }

        protected abstract void init();

        public abstract IEnumerator LoadStep();

        public abstract void UnloadStep();
    }
}