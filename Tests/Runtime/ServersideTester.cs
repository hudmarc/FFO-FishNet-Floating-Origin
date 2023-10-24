using System;
using System.Collections;
using System.Text;
using FishNet.FloatingOrigin;
using FishNet.Managing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
/// <summary>
/// These tests require a manually connected client to function.
/// </summary>
public class ServersideTester
{
    private const float OFFSET_DISTANCE = 149597870700;//one astronomical unit

    [UnityTest]
    public IEnumerator MergeUntilFailServer()
    {
        yield return SetupAndAwaitNetwork();

        Debug.Log("You have 5 seconds to add a client!");
        yield return new WaitForSeconds(5);

        FOView[] views = null;
        FOObject origin = null;

        while (views == null || origin == null)
        {
            views = UnityEngine.Object.FindObjectsOfType<FOView>();
            origin = UnityEngine.GameObject.Find("Origin")?.GetComponent<FOObject>();
            yield return new WaitForFixedUpdate();
        }

        if (!FishNet.InstanceFinder.IsServer)
        {
            yield break;
        }
        FOView serverView = null;
        FOView clientView = null;

        foreach (FOView view in views)
        {
            if (serverView != null && clientView != null)
            {
                break;
            }
            if (view.networking.IsOwner)
            {
                serverView = view;
            }
            else
            {
                clientView = view;
            }
        }
        Assert.NotNull(clientView);
        Assert.NotNull(serverView);
        yield return ServersideTesterAuto.MergeTest(serverView, clientView);
    }

    [UnityTest]
    public IEnumerator MergeUntilFailClient()
    {
        yield return SetupAndAwaitNetwork();

        Debug.Log("You have 5 seconds to add a client!");
        yield return new WaitForSeconds(5);

        FOView[] views = null;
        FOObject origin = null;

        while (views == null || origin == null)
        {
            views = UnityEngine.Object.FindObjectsOfType<FOView>();
            origin = UnityEngine.GameObject.Find("Origin")?.GetComponent<FOObject>();
            yield return new WaitForFixedUpdate();
        }

        if (!FishNet.InstanceFinder.IsServer)
        {
            yield break;
        }
        FOView serverView = null;
        FOView clientView = null;

        foreach (FOView view in views)
        {
            if (serverView != null && clientView != null)
            {
                break;
            }
            if (view.networking.IsOwner)
            {
                serverView = view;
            }
            else
            {
                clientView = view;
            }
        }
        Assert.NotNull(clientView);
        Assert.NotNull(serverView);
        yield return ServersideTesterAuto.MergeTest(clientView, serverView);
    }

    public IEnumerator SetupAndAwaitNetwork()
    {
        SceneManager.LoadScene(3);
        yield return new WaitForSeconds(2);
        Debug.Log("Finished loading test scene");
        var nm = UnityEngine.Object.FindObjectOfType<NetworkManager>();
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.


        nm.ServerManager.StartConnection();

        while (nm.ServerManager.Started == false)
        {
            yield return new WaitForFixedUpdate();
        }
        nm.ClientManager.StartConnection();

        while (nm.ClientManager.Started == false)
        {
            yield return new WaitForFixedUpdate();
        }
    }
}
