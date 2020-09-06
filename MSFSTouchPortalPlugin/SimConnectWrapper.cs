﻿using Microsoft.FlightSimulator.SimConnect;
using MSFSTouchPortalPlugin.Objects.AutoPilot;
using MSFSTouchPortalPlugin.Objects.InstrumentsSystems;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MSFSTouchPortalPlugin {
  /// <summary>
  /// Wrapper for SimConnect
  /// </summary>
  public class SimConnectWrapper {
    public enum Groups {
      System = 0,
      AutoPilot = 1,
      Fuel = 2
    }

    public enum Events {
      StartupMessage = 0
    }

    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    const uint NOTIFICATION_PRIORITY = 10000000;
    const int WM_USER_SIMCONNECT = 0x0402;
    SimConnect _simConnect = null;
    EventWaitHandle _scReady = new EventWaitHandle(false, EventResetMode.AutoReset);
    public bool Connected = false;

    public SimConnectWrapper() { }

    public void Connect() {
      Console.WriteLine("Connect SimConnect");

      try {
        _simConnect = new SimConnect("Touch Portal Plugin", GetConsoleWindow(), WM_USER_SIMCONNECT, _scReady, 0);

        Connected = true;

        // System Events
        _simConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(simconnect_OnRecvOpen);
        _simConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(simconnect_OnRecvQuit);
        _simConnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(simconnect_OnRecvException);

        // Sim mapped events
        _simConnect.OnRecvEvent += new SimConnect.RecvEventEventHandler(simconnect_OnRecvEvent);

        // simconnect.OnRecvAssignedObjectId += new SimConnect.RecvAssignedObjectIdEventHandler(simconnect_OnRecvAssignedObjectId);

        // Sim Data
        _simConnect.OnRecvSimobjectData += new SimConnect.RecvSimobjectDataEventHandler(simconnect_OnRecvSimObjectData);
        _simConnect.OnRecvSimobjectDataBytype += new SimConnect.RecvSimobjectDataBytypeEventHandler(simconnect_OnRecvSimobjectDataBytype);

        _simConnect.ClearNotificationGroup(Groups.System);
        _simConnect.SetNotificationGroupPriority(Groups.System, NOTIFICATION_PRIORITY);

        _simConnect.ClearNotificationGroup(Groups.AutoPilot);
        _simConnect.SetNotificationGroupPriority(Groups.AutoPilot, NOTIFICATION_PRIORITY);

        _simConnect.ClearNotificationGroup(Groups.Fuel);
        _simConnect.SetNotificationGroupPriority(Groups.Fuel, NOTIFICATION_PRIORITY);

        _simConnect.Text(SIMCONNECT_TEXT_TYPE.PRINT_BLACK, 5, Events.StartupMessage, "TouchPortal Connected");
      } catch (COMException ex) {
        Console.WriteLine("Connection to Sim failed: " + ex.Message);
      }
    }

    public void Disconnect() {
      Console.WriteLine("Disconnect SimConnect");

      if (_simConnect != null) {
        /// Dispose serves the same purpose as SimConnect_Close()
        _simConnect.Dispose();
        _simConnect = null;
      }

      Connected = false;
    }

    public Task WaitForMessage() {
      while (true) {
        _scReady.WaitOne();

        // TODO: Exception on quit
        _simConnect.ReceiveMessage();
        //simconnect.RequestDataOnSimObjectType(Events.Test, Group.Test, 0, SIMCONNECT_SIMOBJECT_TYPE.AIRCRAFT);
      }
    }

    public void MapClientEventToSimEvent(Enum eventId, string eventName) {
      if (Connected) {
        _simConnect.MapClientEventToSimEvent(eventId, eventName);
      }
    }

    public void TransmitClientEvent(Groups group, Enum eventId, uint data) {
      if (Connected) {
        _simConnect.TransmitClientEvent((uint)SimConnect.SIMCONNECT_OBJECT_ID_USER, eventId, data, group, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
      }
    }

    public void AddNotification(Enum group, Enum eventId) {
      if (Connected) {
        _simConnect.AddClientEventToNotificationGroup(group, eventId, false);
      }
    }

    private void simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data) {
      Console.WriteLine("Quit");
    }

    private void simconnect_OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data) {
      Console.WriteLine("ReceivedObjectDataByType");
    }

    private void simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data) {
      Console.WriteLine("Opened");
    }

    private void simconnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data) {
      SIMCONNECT_EXCEPTION eException = (SIMCONNECT_EXCEPTION)data.dwException;
      Console.WriteLine("SimConnect_OnRecvException: " + eException.ToString());
    }

    //private void simconnect_OnRecvAssignedObjectId(SimConnect sender, SIMCONNECT_RECV_ASSIGNED_OBJECT_ID data) {
    //  Console.WriteLine("Recieved");
    //}

    /// <summary>
    /// Events triggered by sending events to the Sim
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="data"></param>
    private void simconnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT data) {
      Groups group = (Groups)data.uGroupID;
      dynamic eventId = null;

      switch (group) {
        case Groups.System:
          eventId = (Events)data.uEventID;
          break;
        case Groups.AutoPilot:
          eventId = (AutoPilot)data.uEventID;
          break;
        case Groups.Fuel:
          eventId = (Fuel)data.uEventID;
          break;
      }

      Console.WriteLine($"{DateTime.Now} Recieved: {group} - {eventId}");
    }

    private void simconnect_OnRecvSimObjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data) {
      Console.WriteLine("Recieved");
    }
  }
}