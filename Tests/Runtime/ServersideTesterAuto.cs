using System;
using System.Collections;
using System.Text;
using FishNet;
using FishNet.FloatingOrigin;
using FishNet.Managing;
using FishNet.Managing.Server;
using FishNet.Object;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// These tests can be run automatically on the server and do not require a client connection. TODO: this isn't actually possible at the moment, I need to be manually spawing the main FOView inside of the setup functionhere..
/// </summary>
public class ServersideTesterAuto
{
    private const float OFFSET_DISTANCE = 149597870700; //approximately one astronomical unit
    private const float TEST_ITERATIONS = 600; //standard number of iterations for all tests

    [UnityTest]
    public IEnumerator OffsetTest()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Step; Error (mm);Error at Origin (meters); Distance; Delta");

        yield return SetupAndAwaitNetwork();

        FOView view = null;
        FOObject origin = null;

        while (view == null || origin == null)
        {
            view = UnityEngine.Object.FindObjectOfType<FOView>();
            origin = UnityEngine.GameObject.Find("Origin")?.GetComponent<FOObject>();
            yield return new WaitForFixedUpdate();
        }

        Vector3d position = (Vector3d)view.transform.position;

        yield return new WaitForSeconds(2);
        Debug.Log("Starting test");

        var val = 1;
        for (int i = 0; i < TEST_ITERATIONS; i++)
        {
            // Debug.Log(i);
            Vector3 delta = new Vector3(val, val, val);
            view.transform.position += delta;
            position += (Vector3d)delta;

            if ((val * 2) > 0) //prevent overflow
                val *= 2;

            var error = Vector3d.Distance(position, view.realPosition);
            Assert.IsTrue(error < 0.01);
            var distance_from_origin = Vector3d.Distance(Vector3d.zero, view.realPosition);
            var error_at_origin = Vector3d.Distance(Vector3d.zero, origin.realPosition);
            yield return new WaitForFixedUpdate();
            sb.Append(i);
            sb.Append(";");
            sb.Append((error * 1000));
            sb.Append(";");
            sb.Append(error_at_origin);
            sb.Append(";");
            sb.Append(distance_from_origin);
            sb.Append(";");
            sb.AppendLine(val.ToString());
        }
        Debug.Log("--------RESULTS--------");

        System.IO.File.WriteAllText(Application.persistentDataPath + "/output.csv", sb.ToString());
        System.Diagnostics.Process.Start(Application.persistentDataPath);
    }

    [UnityTest]
    public IEnumerator ErrorAccumulator()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Step; Error (mm);Error At Origin (meters); Distance From Origin; Position Before Rebase");

        yield return SetupAndAwaitNetwork();

        FOView view = null;
        FOObject origin = null;

        while (view == null || origin == null)
        {
            view = UnityEngine.Object.FindObjectOfType<FOView>();
            origin = UnityEngine.GameObject.Find("Origin")?.GetComponent<FOObject>();
            yield return new WaitForSeconds(2);
        }

        Vector3d position = (Vector3d)view.transform.position;

        yield return new WaitForSeconds(2);
        Debug.Log("Starting test");
        for (int i = 0; i < 600; i++)
        {
            Vector3 delta = (i % 2 == 0 ? -1 : 1) * OFFSET_DISTANCE * Vector3.right;
            view.transform.position += delta;

            position += (Vector3d)delta;

            yield return new WaitForFixedUpdate();

            var error = Vector3d.Distance(position, view.realPosition);
            Assert.IsTrue(error < 0.01);
            var distance_from_origin = Vector3.Distance(view.transform.position, Vector3.zero);
            var error_at_origin = Vector3d.Distance(Vector3d.zero, origin.realPosition);

            sb.Append(i);
            sb.Append(";");
            sb.Append(error * 1000);
            sb.Append(";");
            sb.Append(error_at_origin);
            sb.Append(";");
            sb.Append(distance_from_origin);
            sb.Append(";");
            sb.AppendLine(view.transform.position.ToString());


            // Assert.IsTrue( < 0.01);
        }
        Debug.Log("--------RESULTS--------");

        System.IO.File.WriteAllText(Application.persistentDataPath + "/error_accumulator_output.csv", sb.ToString());
        System.Diagnostics.Process.Start(Application.persistentDataPath);
    }

    [UnityTest]
    public IEnumerator MultipleViewsSameClient()
    {
        // StringBuilder sb = new StringBuilder();
        // sb.AppendLine("Step; Error (mm); Distance; Delta");

        yield return SetupAndAwaitNetwork();

        //spawn multiple views

        FOView[] views = new FOView[8];

        FOView view_init = null;

        while (view_init == null)
        {
            view_init = UnityEngine.Object.FindObjectOfType<FOView>();
            yield return new WaitForSeconds(2);
        }

        views[0] = view_init;

        var gob = view_init.gameObject;

        for (int i = 1; i < views.Length; i++)
        {
            views[i] = GameObject.Instantiate(gob).GetComponent<FOView>();
            FishNet.InstanceFinder.NetworkManager.ServerManager.Spawn(views[i].GetComponent<NetworkObject>());
        }

        Vector3d position = (Vector3d)views[0].transform.position;

        yield return new WaitForSeconds(2);
        Debug.Log("Starting test");

        var val = 1;
        for (int i = 0; i < TEST_ITERATIONS; i++)
        {
            int view = i % views.Length;
            Vector3 delta = new Vector3(val, val, val);
            views[view].transform.position += delta;
            position += (Vector3d)delta;

            if ((val * 2) > 0)
                val *= 2;

            // var error = Vector3d.Distance(position, views[0].realPosition);
            // var distance_from_origin = Vector3d.Distance(Vector3d.zero, views[0].realPosition);

            yield return new WaitForFixedUpdate();
            // sb.Append(i);
            // sb.Append(";");
            // sb.Append((error * 1000));
            // sb.Append(";");
            // sb.Append(distance_from_origin);
            // sb.Append(";");
            // sb.AppendLine(val.ToString());
        }
        // Debug.Log("--------RESULTS--------");

        // System.IO.File.WriteAllText(Application.persistentDataPath + "/output.csv", sb.ToString());
        // System.Diagnostics.Process.Start(Application.persistentDataPath);
    }

    [UnityTest]
    public IEnumerator FOObjectGroupChange()
    {
        // StringBuilder sb = new StringBuilder();
        // sb.AppendLine("Step; Error (mm); Distance; Delta");

        yield return SetupAndAwaitNetwork();

        //spawn multiple views

        FOView[] views = new FOView[2];

        FOView view_init = null;

        FOObject foobject = null;

        while (view_init == null || foobject == null)
        {
            view_init = UnityEngine.Object.FindObjectOfType<FOView>();
            FOObject[] objects = UnityEngine.Object.FindObjectsOfType<FOObject>();

            foreach (var obj in objects)
            {
                if (obj.GetType() != typeof(FOView))
                {
                    foobject = obj;
                }
            }
            yield return new WaitForSeconds(2);
        }

        views[0] = view_init;

        var gob = view_init.gameObject;

        for (int i = 1; i < views.Length; i++)
        {
            views[i] = GameObject.Instantiate(gob).GetComponent<FOView>();
            FishNet.InstanceFinder.NetworkManager.ServerManager.Spawn(views[i].GetComponent<NetworkObject>());
        }

        foobject.transform.position = Vector3.one; //easier to test precision loss this way

        Vector3d control_position = foobject.realPosition;

        yield return new WaitForSeconds(2);
        Debug.Log("Starting test");

        //move both views to separate sides of the FOObject

        views[0].transform.position = Vector3.right * OFFSET_DISTANCE;
        views[1].transform.position = Vector3.left * OFFSET_DISTANCE;

        //inside the for loop, move one view to the FOObject, then move it away, then switch to the other FOView and do the same

        bool firstIsSelected = true;

        for (int i = 0; i < 60; i++)
        {
            Debug.Log($"Iteration {i} started");
            FOView view = views[firstIsSelected ? 0 : 1];

            view.SetRealPositionApproximate(view.realPosition.sqrMagnitude > 0 ? Vector3d.zero : (firstIsSelected ? Vector3d.right : Vector3d.left) * OFFSET_DISTANCE);

            if (view.realPosition.sqrMagnitude > 0) //was at zero, is no longer at zero, switch active
            {
                firstIsSelected = !firstIsSelected;
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
                //assert after each loop (with a reasonable delay) that 

                view = views[firstIsSelected ? 0 : 1];

                //sanity check (if this fails the test is invalid and either the delay for waiting for rebase is too short, the wrong FOObject is selected, or something is wrong with the package)
                Debug.Log($"Iteration {i} finished. Checking assertions.");
                if (view.realPosition.sqrMagnitude > 0)
                {
                    throw new Exception("Test invalid! Selected view is not at 0,0,0 real!");
                }

                // A) the FOView in range of the FOObject is in the same scene as the FOObject

                Assert.AreEqual(view.gameObject.scene.handle, foobject.gameObject.scene.handle);

                // B) the FOObject's real position remains mostly unchanged
                Assert.True((control_position - foobject.realPosition).magnitude < 10);

                Debug.Log($"Assertions for Iteration {i} passed");
            }
        }

        bool together = true;

        //test wandering agents which meet at a single point

        for (int i = 0; i < 60; i++)
        {
            views[0].SetRealPositionApproximate(Vector3d.right * (together ? 0 : OFFSET_DISTANCE));
            views[1].SetRealPositionApproximate(Vector3d.left * (together ? 0 : OFFSET_DISTANCE));
            //reasonable delay to let FFO do its thing
            yield return new WaitForSeconds(0.1f);

            if (together)
            {
                // Debug.Break();
                Assert.AreEqual(views[0].gameObject.scene.handle, views[1].gameObject.scene.handle);
                Assert.AreEqual(views[0].gameObject.scene.handle, foobject.gameObject.scene.handle);
            }
            else
            {
                Assert.AreNotEqual(views[0].gameObject.scene.handle, views[1].gameObject.scene.handle);
                //the FOObject will end up in one of the other scenes
                Assert.True(views[0].gameObject.scene.handle == foobject.gameObject.scene.handle || views[1].gameObject.scene.handle == foobject.gameObject.scene.handle);
                // the FOObject's real position remains mostly unchanged
                Assert.True((control_position - foobject.realPosition).magnitude < 10);
            }
            together = !together;
        }
        Debug.Log($"Final real position of foobject: {foobject.realPosition}");
    }

    [UnityTest]
    public IEnumerator StragglersVsGroup()
    {

        yield return SetupAndAwaitNetwork();

        //spawn multiple views

        FOView[] views = new FOView[3];

        FOView view_init = null;

        FOObject foobject = null;

        while (view_init == null || foobject == null)
        {
            view_init = UnityEngine.Object.FindObjectOfType<FOView>();
            FOObject[] objects = UnityEngine.Object.FindObjectsOfType<FOObject>();

            foreach (var obj in objects)
            {
                if (obj.GetType() != typeof(FOView))
                {
                    foobject = obj;
                }
            }
            yield return new WaitForSeconds(2);
        }

        views[0] = view_init;

        var gob = view_init.gameObject;

        for (int i = 1; i < views.Length; i++)
        {
            views[i] = GameObject.Instantiate(gob).GetComponent<FOView>();
            FishNet.InstanceFinder.NetworkManager.ServerManager.Spawn(views[i].GetComponent<NetworkObject>());
        }

        foobject.transform.position = Vector3.one; //easier to test precision loss this way

        Vector3d control_position = foobject.realPosition;

        yield return new WaitForSeconds(2);
        Debug.Log("Starting test");

        // Assert all views start in the same scene

        Assert.AreEqual(views[0].gameObject.scene.handle, views[1].gameObject.scene.handle);
        int unrelatedScenes = SceneManager.sceneCount - 1; //accounts for the 1 existing scene
        // Move the first two views in a direction far past rebase point
        for (int i = 0; i < TEST_ITERATIONS; i++)
        {
            // while looping, assert the two views remain in the same scene
            views[0].transform.position += Vector3.right * 100;
            views[1].transform.position += Vector3.right * 100;
            Assert.AreEqual(views[0].gameObject.scene.handle, views[1].gameObject.scene.handle);

            yield return null;

            // Assert.IsTrue((SceneManager.sceneCount - unrelatedScenes) <= 2);
            if (views[0].gameObject.scene.handle != views[1].gameObject.scene.handle)
                Debug.LogError($"Scene {views[0].gameObject.scene.ToHex()} is not {views[1].gameObject.scene.ToHex()}");

            Assert.AreEqual(views[0].gameObject.scene.handle, views[1].gameObject.scene.handle);

        }

        // after the loop, assert the two views are in a different scene to the first view, and the object is in the same scene as the first view
        Assert.AreEqual(views[0].gameObject.scene.handle, views[1].gameObject.scene.handle);
        Assert.AreNotEqual(views[0].gameObject.scene.handle, views[2].gameObject.scene.handle);
        Assert.AreEqual(views[2].gameObject.scene.handle, foobject.gameObject.scene.handle);
    }
    static uint lastTick = 0;
    public static IEnumerator MergeTest(FOView test, FOView control)
    {

        test.transform.position = Vector3.zero;
        control.transform.position = Vector3.zero;

        Assert.AreEqual(control.gameObject.scene, test.gameObject.scene);
        Debug.Log("Starting test");
        Debug.Log($"Test: {test.networking.ObjectId}");
        Debug.Log($"Control: {control.networking.ObjectId}");

        Vector3d controlReal = control.realPosition;

        Debug.Log($"Initial Unity Position: {control.transform.position} Inital Real: {control.realPosition} ");
        Vector3 move = Vector3.zero;
        int desyncFrameCount = 0;

        for (int i = 0; i < TEST_ITERATIONS; i++)
        {
            Debug.Log($"---------- Merge {i} ----------");
            if (i % 2 != 0)
            {
                move = new Vector3(((i % 29) * OFFSET_DISTANCE) + i, ((i % 31) * OFFSET_DISTANCE) + i, ((i % 37) * OFFSET_DISTANCE) + i);
                if (test.realPosition != Vector3d.zero)
                {
                    test.transform.position = FOManager.instance.RealToUnity(Vector3d.zero, test.gameObject.scene);
                }
                Debug.Log($"BEFORE Test: {test.transform.position} Control: {control.transform.position} Test Real: {test.realPosition} Control Real: {control.realPosition}");
            }
            else
            {
                move = -move;
            }
            test.transform.position += move;

            Debug.Log("Dist During: " + Vector3d.Magnitude(controlReal - control.realPosition).ToString("########.############"));

            //the whole point of this test is that since the control does not move this should never be true unless something is wrong with the offsetting
            if (Vector3d.Magnitude(controlReal - control.realPosition) > 10)
            {
                Debug.Log($"Desynchronized! Dist: {Vector3d.Magnitude(controlReal - control.realPosition)} Merges: {i} Group: {control.gameObject.scene.ToHex()} Offset: {FOManager.instance.UnityToReal(Vector3.zero, control.gameObject.scene)} Unity Position: {control.transform.position} Real: {control.realPosition} Expected: {controlReal}");
                desyncFrameCount++;
            }
            else
            {
                desyncFrameCount = 0;
            }

            if (desyncFrameCount > 5)
            {
                throw new Exception("Desynchronization lasted for more than 5 frames!");
            }

            //waits until end of tick
            // while (InstanceFinder.NetworkManager.TimeManager.Tick <= lastTick)
            // {
            //     yield return null;
            // }
            // lastTick = InstanceFinder.NetworkManager.TimeManager.Tick;

            yield return null;

            Debug.Log($"AFTER Test: {test.transform.position} Control: {control.transform.position} Test Real: {test.realPosition} Control Real: {control.realPosition}");
        }
    }
    [UnityTest]
    public IEnumerator MergeTestOffline()
    {
        // StringBuilder sb = new StringBuilder();
        // sb.AppendLine("Step; Error (mm); Distance; Delta");

        yield return SetupAndAwaitNetwork();

        //spawn multiple views

        FOView[] views = new FOView[2];

        FOView view_init = null;

        FOObject foobject = null;

        while (view_init == null || foobject == null)
        {
            view_init = UnityEngine.Object.FindObjectOfType<FOView>();
            FOObject[] objects = UnityEngine.Object.FindObjectsOfType<FOObject>();

            foreach (var obj in objects)
            {
                if (obj.GetType() != typeof(FOView))
                {
                    foobject = obj;
                }
            }
            yield return new WaitForSeconds(2);
        }

        views[0] = view_init;

        var gob = view_init.gameObject;

        for (int i = 1; i < views.Length; i++)
        {
            views[i] = GameObject.Instantiate(gob).GetComponent<FOView>();
            FishNet.InstanceFinder.NetworkManager.ServerManager.Spawn(views[i].GetComponent<NetworkObject>());
        }

        foobject.transform.position = Vector3.one; //easier to test precision loss this way

        Vector3d control_position = foobject.realPosition;

        yield return new WaitForSeconds(1);
        Debug.Log("Starting test");

        yield return MergeTest(views[0], views[1]);
    }

    public IEnumerator SetupAndAwaitNetwork()
    {
        SceneManager.LoadScene(3);
        // if (SceneManager.sceneCount > 1 && !SceneManager.GetSceneAt(0).name.Contains("InitTestScene"))
        //     SceneManager.UnloadSceneAsync(0);

        yield return new WaitForSeconds(2);
        Debug.Log("Finished loading test scene");
        var nm = UnityEngine.Object.FindObjectOfType<NetworkManager>();
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.

        Debug.Log("Awaiting server connection");
        nm.ServerManager.StartConnection();

        while (nm.ServerManager.Started == false)
        {
            yield return new WaitForFixedUpdate();
        }
        Debug.Log("Started server connection");
        Debug.Log("Awaiting client connection");
        nm.ClientManager.StartConnection();

        while (nm.ClientManager.Started == false)
        {
            yield return new WaitForFixedUpdate();
        }
        Debug.Log("Started client connection");
    }
}
