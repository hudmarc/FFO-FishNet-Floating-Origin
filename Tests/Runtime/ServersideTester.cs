using System.Collections;
using FloatingOffset.Runtime;
using FishNet.Object;
using FloatingOffset.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
/// <summary>
/// These tests require a manually connected client to function.
/// The default time alloted to connect a client is 10 seconds,
/// you may change this by modifying WAIT_FOR_CLIENT if you need more time,
/// assuming you have installed the package in a way that lets you modify its scripts.
/// </summary>
public class ServersideTester
{
    /// <summary>
    /// Default time to wait for client is 10 seconds.
    /// </summary>
    private const float WAIT_FOR_CLIENT = 10;
    /// <summary>
    /// Networked test setup for the MergeTest. In this case the active OffsetView is the server's view.
    /// You must manually add another game client, which should simply connect as a client. I recommend using
    /// a tool like Parrelsync to test this locally.
    /// </summary>
    [UnityTest]
    public IEnumerator NetworkedMergeUntilFailServer()
    {
        yield return ServersideTesterAuto.SetupAndAwaitNetwork();

        Debug.Log($"You have ${WAIT_FOR_CLIENT} seconds to add a client!");
        yield return new WaitForSeconds(WAIT_FOR_CLIENT);

        OffsetView[] views = null;
        OffsetTransform origin = null;

        while (views == null || origin == null)
        {
            views = Object.FindObjectsOfType<OffsetView>();
            origin = GameObject.Find("Origin")?.GetComponent<OffsetTransform>();
            yield return new WaitForFixedUpdate();
        }

        if (!FishNet.InstanceFinder.IsServer)
        {
            yield break;
        }
        OffsetView serverView = null;
        OffsetView clientView = null;

        foreach (OffsetView view in views)
        {
            if (serverView != null && clientView != null)
            {
                break;
            }
            if (view.GetComponent<NetworkObject>().IsOwner)
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

        yield return ServersideTesterAuto.Cleanup();
    }
    /// <summary>
    /// Networked test setup for the MergeTest. In this case the active OffsetView is the client's view.
    /// You must manually add another game client, which should simply connect as a client. I recommend using
    /// a tool like Parrelsync to test this locally.
    /// </summary>
    [UnityTest]
    public IEnumerator NetworkedMergeUntilFailClient()
    {
        yield return ServersideTesterAuto.SetupAndAwaitNetwork();

        Debug.Log($"You have ${WAIT_FOR_CLIENT} seconds to add a client!");
        yield return new WaitForSeconds(WAIT_FOR_CLIENT);

        OffsetView[] views = null;
        OffsetTransform origin = null;

        while (views == null || origin == null)
        {
            views = UnityEngine.Object.FindObjectsOfType<OffsetView>();
            origin = UnityEngine.GameObject.Find("Origin")?.GetComponent<OffsetTransform>();
            yield return new WaitForFixedUpdate();
        }

        if (!FishNet.InstanceFinder.IsServer)
        {
            yield break;
        }
        OffsetView serverView = null;
        OffsetView clientView = null;

        foreach (OffsetView view in views)
        {
            if (serverView != null && clientView != null)
            {
                break;
            }
            if (view.GetComponent<NetworkObject>().IsOwner)
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

        yield return ServersideTesterAuto.Cleanup();
    }
}
