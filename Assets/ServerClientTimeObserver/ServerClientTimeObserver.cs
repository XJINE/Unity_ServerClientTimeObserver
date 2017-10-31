using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;


// NOTE:
// channel 1 が Unrealiable である必要があります。
// 

/// <summary>
/// サーバーとクライアント間の時間を観測します。
/// </summary>
public class ServerClientTimeObserver : MonoBehaviour
{
    #region Class

    /// <summary>
    /// サーバーとクライアントの時間を記録するためのメッセージタイプ。
    /// </summary>
    public class ServerClientTimeMessageType
    {
        /// <summary>
        /// 到達時間を計測するタイプ。
        /// </summary>
        public const short CheckReachTime = MsgType.Highest + 1;
    }

    /// <summary>
    /// サーバーとクライアントの時間を記録するメッセージ。
    /// </summary>
    public class ServerClientTimeMessage : MessageBase
    {
        /// <summary>
        /// サーバーの送信時間。
        /// </summary>
        public double serverSendTime;

        /// <summary>
        /// クライアントの受信時間(現在のところ利用されません)。
        /// </summary>
        public double clientReceiveTime;

        /// <summary>
        /// サーバーの受信時間。
        /// </summary>
        public double serverReceiveTime;
    }

    /// <summary>
    /// サーバーとクライアント間の時間を示すデータ。
    /// </summary>
    public class ServerClientTimeData
    {
        #region Field

        /// <summary>
        /// バッファのサイズ(保存しておくデータの数)。
        /// </summary>
        protected int bufferSize;

        /// <summary>
        /// サーバーからサーバーに帰るまでにかかった時間のバッファ。
        /// </summary>
        protected Queue<double> serverToServerTimesQueue;

        #endregion Field

        #region Constructor

        /// <summary>
        /// 新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="bufferSize">
        /// バッファサイズ(保存しておくデータの数)。
        /// </param>
        public ServerClientTimeData(int bufferSize)
        {
            this.bufferSize = bufferSize;
            this.serverToServerTimesQueue = new Queue<double>(bufferSize);
        }

        #endregion Constructor

        #region Method

        /// <summary>
        /// 新しいデータを追加して更新します。
        /// </summary>
        /// <param name="serverClientTimeMessage">
        /// 新しいデータ。
        /// </param>
        public void Update(ServerClientTimeMessage serverClientTimeMessage)
        {
            this.serverToServerTimesQueue.Enqueue(serverClientTimeMessage.serverReceiveTime - serverClientTimeMessage.serverSendTime);

            while (this.serverToServerTimesQueue.Count > this.bufferSize)
            {
                this.serverToServerTimesQueue.Dequeue();
            }
        }

        /// <summary>
        /// サーバーからサーバーに帰るまでにかかった時間の平均を取得します。
        /// </summary>
        /// <returns>
        /// サーバーからサーバーに帰るまでにかかった時間の平均。
        /// </returns>
        public double GetServerToServerTimeAverage()
        {
            return this.serverToServerTimesQueue.Average();
        }

        /// <summary>
        /// サーバーからサーバーに帰るまでにかかった最新の時間を取得します。
        /// </summary>
        /// <returns>
        /// サーバーからサーバーに帰るまでにかかった最新の時間。
        /// </returns>
        public double GetServerToServerTimeLatest()
        {
            return this.serverToServerTimesQueue.LastOrDefault();
        }

        #endregion Method
    }

    /// <summary>
    /// サーバーとクライアント間の時間を計測するための管理クラス。
    /// </summary>
    public class ServerClientTimeDataManager
    {
        #region Field

        /// <summary>
        /// エラーが起きたときの時間の値。
        /// </summary>
        public static double ErrorTimeValue = -1;

        /// <summary>
        /// ServerClientTimeData に保存しておくデータの数。
        /// </summary>
        protected int bufferSize;

        /// <summary>
        /// ID をキーとして、サーバーとクライアント間の時間を管理する辞書。
        /// </summary>
        protected Dictionary<int, ServerClientTimeData> serverClientTimeDataDictionary;

        #endregion Field

        #region Constructor

        /// <summary>
        /// 新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="bufferSize">
        /// クライアントあたりに保存しておくデータの数。
        /// </param>
        public ServerClientTimeDataManager(int bufferSize)
        {
            this.bufferSize = bufferSize;
            this.serverClientTimeDataDictionary = new Dictionary<int, ServerClientTimeData>();
        }

        #endregion Constructor

        #region Method

        /// <summary>
        /// 既存のクライアントのとき、データを更新します。新しいクライアントのとき、データを追加して更新します。
        /// </summary>
        /// <param name="clientId">
        /// データを更新するクライアントのID。
        /// </param>
        /// <param name="serverClientTimeMessage">
        /// 更新するデータ。
        /// </param>
        public void Update(int clientId, ServerClientTimeMessage serverClientTimeMessage)
        {
            if (!this.serverClientTimeDataDictionary.ContainsKey(clientId))
            {
                this.serverClientTimeDataDictionary.Add(clientId, new ServerClientTimeData(this.bufferSize));
            }

            this.serverClientTimeDataDictionary[clientId].Update(serverClientTimeMessage);
        }

        /// <summary>
        /// 既存のクライアントに関するデータを削除します。
        /// </summary>
        /// <param name="clientId">
        /// データを削除するクライアントのID。
        /// </param>
        /// <returns>
        /// 削除に成功するときtrue, 失敗するときfalse。
        /// </returns>
        public bool Remove(int clientId)
        {
            return this.serverClientTimeDataDictionary.Remove(clientId);
        }

        /// <summary>
        /// サーバーからサーバーに帰るまでにかかった時間の平均を取得します。
        /// </summary>
        /// <returns>
        /// サーバーからサーバーに帰るまでにかかった時間の平均。
        /// キーが登録されていないとき、ServerClientTimeDataManager.ErrorTimeValue。
        /// </returns>
        public double GetServerToServerTimeAverage(int clientId)
        {
            ServerClientTimeData data;

            if (this.serverClientTimeDataDictionary.TryGetValue(clientId, out data))
            {
                return data.GetServerToServerTimeAverage();
            }
            else
            {
                return ServerClientTimeDataManager.ErrorTimeValue;
            }
        }

        /// <summary>
        /// サーバーからサーバーに帰るまでにかかった最新の時間を取得します。
        /// </summary>
        /// <returns>
        /// サーバーからサーバーに帰るまでにかかった最新の時間。
        /// キーが登録されていないとき、ServerClientTimeDataManager.ErrorTimeValue。
        /// </returns>
        public double GetServerToServerTimeLatest(int clientId)
        {
            ServerClientTimeData data;

            if (this.serverClientTimeDataDictionary.TryGetValue(clientId, out data))
            {
                return data.GetServerToServerTimeLatest();
            }
            else
            {
                return ServerClientTimeDataManager.ErrorTimeValue;
            }
        }

        #endregion Method
    }

    #endregion Class

    #region Field

    /// <summary>
    /// マネージャーが保存するデータの数(クライアントあたり)。
    /// </summary>
    public int bufferSize = 8;

    /// <summary>
    /// サーバーとクライアント間の時間を計測する間隔。
    /// </summary>
    public float intervalTimeSec = 0.3f;

    /// <summary>
    /// サーバーとクライアント間の時間を計測する間隔のカウンタ。
    /// </summary>
    protected float intervalTimeSecCounter = 0;

    /// <summary>
    /// サーバーとクライアント間の時間を計測するためのマネージャ。
    /// </summary>
    public ServerClientTimeDataManager serverClientTimeDataManager;

    /// <summary>
    /// Unrealiable で送信されるチャネル。
    /// </summary>
    protected int unreliableChannel = 1;

    #endregion Field

    #region Method

    /// <summary>
    /// 初期化時に呼び出されます。
    /// </summary>
    protected void Awake()
    {
        this.serverClientTimeDataManager = new ServerClientTimeDataManager(this.bufferSize);
    }

    /// <summary>
    /// 開始時に呼び出されます。
    /// </summary>
    protected void Start()
    {
        SetUnrealiableChannel();
        SetServerSettings();
        SetClientSettings();
    }

    /// <summary>
    /// 更新時に呼び出されます。
    /// </summary>
    protected void Update()
    {
        this.intervalTimeSecCounter += Time.deltaTime;
        if (this.intervalTimeSecCounter > this.intervalTimeSec)
        {
            SendServerClientTimeCheckMessages();
            this.intervalTimeSecCounter = 0;
        }
    }

    /// <summary>
    /// Unrealiable な Channel を検出または設定します。
    /// </summary>
    protected void SetUnrealiableChannel()
    {
        this.unreliableChannel = CustomNetworkManager.singleton.channels.FindIndex((channel) =>
        {
            return channel == QosType.Unreliable;
        });

        if (this.unreliableChannel == -1)
        {
            CustomNetworkManager.singleton.channels.Add(QosType.Unreliable);
            this.unreliableChannel = CustomNetworkManager.singleton.channels.Count - 1;
        }
    }

    /// <summary>
    /// クライアントに必要な設定をします。
    /// </summary>
    protected void SetClientSettings()
    {
        // クライアントが開始されたタイミングで実行します。
        // クライアントがメッセージを受信したら、時間を記録して、サーバーにメッセージを送り返すように設定します。

        CustomNetworkManager.singleton.StartClientEventHandler.AddListener((client) =>
        {
            client.RegisterHandler(ServerClientTimeMessageType.CheckReachTime, (networkMessage) =>
            {
                ServerClientTimeMessage messageEntity = networkMessage.ReadMessage<ServerClientTimeMessage>();
                messageEntity.clientReceiveTime = Network.time;
                networkMessage.conn.Send(ServerClientTimeMessageType.CheckReachTime, messageEntity);
            });
        });
    }

    /// <summary>
    /// サーバーに必要な設定をします。
    /// </summary>
    protected void SetServerSettings()
    {
        // サーバーでクライアントが接続されたときは、時間を計測するメッセージを送信します。

        CustomNetworkManager.singleton.ServerConnectEventHandler.AddListener((connection) =>
        {
            NetworkServer.SendToClient(connection.connectionId,
                           ServerClientTimeMessageType.CheckReachTime,
                           new ServerClientTimeMessage()
                           {
                               serverSendTime = Network.time
                           });
        });

        // サーバーでクライアントが切断されたときは、保存してあるデータを削除します。

        CustomNetworkManager.singleton.ServerDisconnectEventHandler.AddListener((connection) =>
        {
            this.serverClientTimeDataManager.Remove(connection.connectionId);
        });

        // サーバーが停止して再開するときに、登録しなおす必要がある点に注意します。
        // サーバーがメッセージを受信したら、時間を記録して更新します。

        CustomNetworkManager.singleton.StartServerEventHandler.AddListener(() =>
        {
            NetworkServer.RegisterHandler(ServerClientTimeMessageType.CheckReachTime, (networkMessage) =>
            {
                ServerClientTimeMessage messageEntity = networkMessage.ReadMessage<ServerClientTimeMessage>();
                messageEntity.serverReceiveTime = Network.time;
                this.serverClientTimeDataManager.Update(networkMessage.conn.connectionId, messageEntity);
            });
        });
    }

    /// <summary>
    /// サーバーとクライアントの時間を計測するためのメッセージを送信します。
    /// </summary>
    public void SendServerClientTimeCheckMessages()
    {
        ServerClientTimeMessage message = new ServerClientTimeMessage()
        {
            serverSendTime = Network.time
        };

        // NOTE:
        // Default の Channel(0) で送信されてしまわないように注意します。
        // NetworkServer.SendToAll(ServerClientTimeMessageType.CheckReachTime, message);
        // UDP(Unrealiable) で送信することを推奨します。

        NetworkServer.SendByChannelToAll(ServerClientTimeMessageType.CheckReachTime, message, this.unreliableChannel);
    }

    #endregion Method
}