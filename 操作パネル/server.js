// server.js
const express = require('express');
const http = require('http');
const WebSocket = require('ws');
const { Client } = require('node-osc');

// サーバーの初期設定
const app = express();
const server = http.createServer(app);
const wss = new WebSocket.Server({ server });

// 1. Live Serverの代わり：同じフォルダにある index.html を配信する
app.use(express.static(__dirname));

// 2. OSC中継処理：タブレットからWebSocketを受け取り、UnityへOSCを投げる
wss.on('connection', (ws) => {
    console.log('📱 タブレットが接続されました');

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
    });
});

// サーバーをポート8080で起動
const PORT = 8080;
server.listen(PORT, '0.0.0.0', () => {
    console.log(`
=========================================
 🧠 脳髄XR オペレーションサーバー起動完了
=========================================
 タブレットのブラウザから以下のURLにアクセスしてください:
 http://<このPCのIPアドレス>:${PORT}
=========================================
    `);
});