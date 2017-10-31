using UnityEngine;
using UnityEngine.Networking;

public class ServerClientTimeObserverSample : MonoBehaviour
{
    #region Field

    public ServerClientTimeObserver serverClientTimeObserver;

    public Rect infoArea;

    #endregion Field

    #region Method

    protected void OnGUI()
    {
        if (NetworkServer.active)
        {
            GUILayout.BeginArea(this.infoArea);

            GUILayout.Label("= TIME DATA =");

            foreach (NetworkConnection connection in NetworkServer.connections)
            {
                // NOTE:
                // connection は null が与えられる可能性があります。
                // NetworkServer 内で管理はされているものの、接続情報が失われた場合などです。
                // またサーバーで開始し、クライアントが接続されると、1 つな null になるようです。
                // https://docs.unity3d.com/ScriptReference/Networking.NetworkServer-connections.html

                if (connection == null)
                {
                    continue;
                }

                GUILayout.Label("ID : " + connection.connectionId);

                GUILayout.Label("Server to Server Avg. : " + this.serverClientTimeObserver.serverClientTimeDataManager
                                                                 .GetServerToServerTimeAverage(connection.connectionId).ToString("F2"));
                GUILayout.Label("Server to Server Lst. : " + this.serverClientTimeObserver.serverClientTimeDataManager
                                                                 .GetServerToServerTimeLatest(connection.connectionId).ToString("F2"));
            }

            GUILayout.EndArea();
        }
    }

    #endregion Method
}