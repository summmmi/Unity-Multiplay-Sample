const express = require('express');
const path = require('path');
const app = express();
const PORT = 8080;

// Unity WebGL 빌드 경로
const BUILD_PATH = '/Users/macmini/Desktop/1-Projects/수퍼 테스트 배드/팀작업/Unity_WebGL/public/Build';

// MIME 타입 설정
express.static.mime.types['wasm'] = 'application/wasm';

// CORS 미들웨어
app.use((req, res, next) => {
  res.header('Access-Control-Allow-Origin', '*');
  res.header('Cross-Origin-Embedder-Policy', 'require-corp');
  res.header('Cross-Origin-Opener-Policy', 'same-origin');
  next();
});

// 정적 파일 서빙
app.use(express.static(BUILD_PATH, {
  setHeaders: (res, path) => {
    if (path.endsWith('.wasm')) {
      res.setHeader('Content-Type', 'application/wasm');
    }
    else if (path.endsWith('.data')) {
      res.setHeader('Content-Type', 'application/octet-stream');
    }
    else if (path.endsWith('.js')) {
      res.setHeader('Content-Type', 'application/javascript');
    }
  }
}));

// 서버 시작
app.listen(PORT, '0.0.0.0', () => {
  console.log('====================================');
  console.log('🎮 Unity WebGL Node.js 서버');
  console.log('====================================');
  console.log(`로컬: http://localhost:${PORT}`);
  console.log(`네트워크: http://192.168.219.105:${PORT}`);
  console.log('====================================');
  console.log('종료: Ctrl+C');
});