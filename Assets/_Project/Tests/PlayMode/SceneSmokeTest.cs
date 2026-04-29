using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Project.Tests.PlayMode
{
    public class SceneSmokeTest
    {
        const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        [UnityTest]
        public IEnumerator MainScene_LoadsAndContainsThreeZones()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);

            var world = GameObject.Find("[World]");
            Assert.NotNull(world, "[World] root must exist in scene");

            var zone1 = world.transform.Find("Zone1");
            var zone2 = world.transform.Find("Zone2");
            var zone3 = world.transform.Find("Zone3");
            Assert.NotNull(zone1, "Zone1 missing");
            Assert.NotNull(zone2, "Zone2 missing");
            Assert.NotNull(zone3, "Zone3 missing");

            Assert.That(zone3.position.x, Is.GreaterThan(zone1.position.x));
        }

        [UnityTest]
        public IEnumerator MainScene_HasMainCameraAndBootstrap()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);

            var cam = Camera.main;
            Assert.NotNull(cam, "Main camera missing");

            var bootstrap = Object.FindFirstObjectByType<Project.Input.MainSceneBootstrap>();
            Assert.NotNull(bootstrap, "MainSceneBootstrap must be present");
        }
    }
}
