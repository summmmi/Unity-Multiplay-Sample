#!/usr/bin/env python3
"""
Simple Unity WebGL Local Server
WASM MIME type 문제를 완전히 해결한 버전
"""
from http.server import HTTPServer, SimpleHTTPRequestHandler
import os
import socket

# 로컬 IP 찾기
def get_local_ip():
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        s.connect(('10.254.254.254', 1))
        ip = s.getsockname()[0]
    except:
        ip = '127.0.0.1'
    finally:
        s.close()
    return ip

# WebGL 빌드 경로
BUILD_PATH = "/Users/macmini/Desktop/1-Projects/수퍼 테스트 배드/팀작업/Unity_WebGL/public/Build"
os.chdir(BUILD_PATH)

# 커스텀 핸들러
class MyHTTPRequestHandler(SimpleHTTPRequestHandler):
    def do_GET(self):
        # WASM 파일 특별 처리
        if self.path.endswith('.wasm') or '.wasm.' in self.path:
            try:
                # 파일 읽기
                file_path = self.path[1:] if self.path.startswith('/') else self.path
                with open(file_path, 'rb') as f:
                    content = f.read()
                
                # 헤더 전송
                self.send_response(200)
                self.send_header('Content-Type', 'application/wasm')
                self.send_header('Content-Length', str(len(content)))
                self.send_header('Access-Control-Allow-Origin', '*')
                self.end_headers()
                
                # 컨텐츠 전송
                self.wfile.write(content)
                return
            except:
                pass
        
        # 다른 파일들은 기본 처리
        return SimpleHTTPRequestHandler.do_GET(self)
    
    def end_headers(self):
        # 모든 요청에 CORS 헤더 추가
        self.send_header('Access-Control-Allow-Origin', '*')
        SimpleHTTPRequestHandler.end_headers(self)

# 서버 시작
PORT = 8080
local_ip = get_local_ip()

print("=" * 50)
print("🎮 Unity WebGL 로컬 서버")
print("=" * 50)
print(f"📁 빌드 경로: {BUILD_PATH}")
print(f"🌐 로컬: http://localhost:{PORT}")
print(f"📱 네트워크: http://{local_ip}:{PORT}")
print("=" * 50)
print("종료: Ctrl+C")
print("=" * 50)

httpd = HTTPServer(('', PORT), MyHTTPRequestHandler)
httpd.serve_forever()