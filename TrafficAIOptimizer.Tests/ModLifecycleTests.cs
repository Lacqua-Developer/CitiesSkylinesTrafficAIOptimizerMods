using System.Collections.Generic;
using ICities;
using NUnit.Framework;

namespace TrafficAIOptimizer.Tests
{
    [TestFixture]
    public class ModLifecycleTests
    {
        private Mod _mod;

        [SetUp]
        public void Setup()
        {
            _mod = new Mod();
        }

        [Test]
        public void GivenModInstance_WhenPropertiesRead_ThenNameAndDescriptionMustBeValid()
        {
            Assert.IsNotNull(_mod.Name);
            Assert.That(_mod.Name, Does.Contain("Traffic AI Optimizer"));

            Assert.IsNotNull(_mod.Description);
            Assert.That(_mod.Description.Length, Is.GreaterThan(10));
        }

        [Test]
        public void GivenModInstance_WhenOnSettingsUIInvokedWithNull_ThenShouldNotThrowException()
        {
            Assert.DoesNotThrow(() => _mod.OnSettingsUI(null));
        }

        [Test]
        public void GivenModInstance_WhenOnSettingsUIInvokedWithHelper_ThenShouldAddOptionsGroup()
        {
            FakeUIHelper fakeHelper = new FakeUIHelper();

            Assert.DoesNotThrow(() => _mod.OnSettingsUI(fakeHelper));
            Assert.That(fakeHelper.AddedGroups, Contains.Item("Traffic AI Optimizer"));
        }

        private class FakeUIHelper : UIHelperBase
        {
            public List<string> AddedGroups { get; } = new List<string>();

            public UIHelperBase AddGroup(string text)
            {
                AddedGroups.Add(text);
                return this;
            }

            public object AddButton(string text, OnButtonClicked eventCallback) => null;
            public object AddCheckbox(string text, bool defaultValue, OnCheckChanged eventCallback) => null;
            public object AddDropdown(string text, string[] options, int defaultSelection, OnDropdownSelectionChanged eventCallback) => null;
            public object AddSlider(string text, float min, float max, float step, float defaultValue, OnValueChanged eventCallback) => null;
            public object AddSpace(int height) => null;
            public object AddTextfield(string text, string defaultContent, OnTextChanged eventCallback, OnTextSubmitted eventSubmitted) => null;
        }
    }
}
