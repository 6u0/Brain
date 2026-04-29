// server.js
const express = require('express');
const http = require('http');
const WebSocket = require('ws');
const { Client, Server } = require('node-osc');

// サーバーの初期設定
const app = express();
const server = http.createServer(app);
const wss = new WebSocket.Server({ server });

const OSC_IN_PORT = 9001;
const oscServer = new Server(OSC_IN_PORT, '0.0.0.0');

const wsClients = new Set();
const latestPanelState = {
    autoConfig: null,
    unityStatus: null,
    phase: null
};

// 1. Live Serverの代わり：同じフォルダにある index.html を配信する
app.use(express.static(__dirname));

// 2. OSC中継処理：タブレットからWebSocketを受け取り、UnityへOSCを投げる
wss.on('connection', (ws) => {
    console.log('📱 タブレットが接続されました');

    wsClients.add(ws);
    sendSnapshot(ws);

    ws.on('message', (message) => {
        try {
            const data = JSON.parse(message);
            
            // UnityへOSCパケットを送信
            const oscClient = new Client(data.ip, data.port);
            
            // 引数がある場合とない場合で処理を分岐
            const args = data.args ? data.args.map(a => a.value) : [];
            oscClient.send(data.address, ...args, () => {
                oscClient.close(); // 送信完了後にクライアントを閉じる
            });
            
            console.log(`➡️ OSC送信完了: IP=${data.ip}:${data.port}, Address=${data.address}, Value=${args}`);

        } catch (error) {
            console.error('❌ メッセージ処理エラー:', error);
        }
    });

    ws.on('close', () => {
        console.log('📱 タブレットとの接続が切れました');
        wsClients.delete(ws);
    });
});

oscServer.on('message', (address, ...args) => {
    const payload = {
        address: address,
        value: args.length > 0 ? normalizeOscArg(args[0]) : null,
        args: args.map(normalizeOscArg)
    };

    updateLatestState(payload);

    for (const client of wsClients) {
        if (client.readyState === WebSocket.OPEN) {
            client.send(JSON.stringify(payload));
        }
    }

    console.log(`⬅️ OSC受信: Address=${address}, Value=${payload.value}`);
});

function normalizeOscArg(arg) {
    if (arg === null || arg === undefined) {
        return null;
    }

    if (typeof arg === 'object' && Object.prototype.hasOwnProperty.call(arg, 'value')) {
        return arg.value;
    }

    return arg;
}

function updateLatestState(payload) {
    if (payload.address === '/panel/auto_config') {
        latestPanelState.autoConfig = payload.args;
        return;
    }

    if (payload.address === '/panel/unity_status') {
        latestPanelState.unityStatus = payload.args;
        return;
    }

    if (payload.address === '/phase/current') {
        latestPanelState.phase = payload.args;
    }
}

function sendSnapshot(ws) {
    if (latestPanelState.autoConfig) {
        ws.send(JSON.stringify({
            address: '/panel/auto_config',
            value: latestPanelState.autoConfig[0] ?? null,
            args: latestPanelState.autoConfig
        }));
    }

    if (latestPanelState.unityStatus) {
        ws.send(JSON.stringify({
            address: '/panel/unity_status',
            value: latestPanelState.unityStatus[0] ?? null,
            args: latestPanelState.unityStatus
        }));
    }

    if (latestPanelState.phase) {
        ws.send(JSON.stringify({
            address: '/phase/current',
            value: latestPanelState.phase[0] ?? null,
            args: latestPanelState.phase
        }));
    }
}

// サーバーをポート8080で起動
const PORT = 8080;
server.listen(PORT, '0.0.0.0', () => {
    console.log(`
=========================================
 🧠 脳髄XR オペレーションサーバー起動完了
=========================================
 タブレットのブラウザから以下のURLにアクセスしてください:
 http://<このPCのIPアドレス>:${PORT}
 OSC受信ポート: ${OSC_IN_PORT}
=========================================
    `);
});