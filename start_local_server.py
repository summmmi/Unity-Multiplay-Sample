#!/usr/bin/env python3
import http.server
import socketserver
import socket
import os
import sys

# 로컬 IP 주소 자동 감지
def get_local_ip():
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        # 실제로 연결하지 않고 IP를 찾기 위한 더미 주소
        s.connect(('10.254.254.254', 1))
        ip = s.getsockname()[0]
    except Exception:
        ip = '127.0.0.1'
    finally:
        s.close()
    return ip

# WebGL 빌드 폴더 확인
WEBGL_BUILD_PATH = "/Users/macmini/Desktop/1-Projects/수퍼 테스트 배드/팀작업/Unity_WebGL/public"
PORT = 8080

if not os.path.exists(WEBGL_BUILD_PATH):
    print(f"❌ WebGL 빌드 폴더가 존재하지 않습니다: {WEBGL_BUILD_PATH}")
    print("Unity에서 WebGL 빌드를 먼저 생성해주세요.")
    sys.exit(1)

# HTTP 서버 설정 (CORS 및 WebGL 최적화)
class WebGLHTTPRequestHandler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=WEBGL_BUILD_PATH, **kwargs)
    
    def end_headers(self):
        # CORS 헤더 추가 (로컬 네트워크 접근 허용)
        self.send_header('Access-Control-Allow-Origin', '*')
        self.send_header('Access-Control-Allow-Methods', 'GET, POST, OPTIONS')
        self.send_header('Access-Control-Allow-Headers', 'Content-Type')
        
        # WebGL 최적화 헤더
        if self.path.endswith('.wasm'):
            self.send_header('Content-Type', 'application/wasm')
        elif self.path.endswith('.data'):
            self.send_header('Content-Type', 'application/octet-stream')
        elif self.path.endswith('.js'):
            self.send_header('Content-Type', 'application/javascript')
        
        super().end_headers()

# 서버 시작
local_ip = get_local_ip()
print("🌐 Unity WebGL 로컬 네트워크 서버")
print("=" * 50)

try:
    with socketserver.TCPServer(("", PORT), WebGLHTTPRequestHandler) as httpd:
        print(f"📁 WebGL 폴더: {WEBGL_BUILD_PATH}")
        print(f"🌍 로컬 접속: http://localhost:{PORT}")
        print(f"📱 네트워크 접속: http://{local_ip}:{PORT}")
        print("=" * 50)
        print("같은 WiFi의 다른 기기에서 네트워크 주소로 접속 가능합니다!")
        print("서버 종료: Ctrl+C")
        print("=" * 50)
        
        httpd.serve_forever()
        
except KeyboardInterrupt:
    print("\\n서버가 종료되었습니다.")
except OSError as e:
    print(f"❌ 포트 {PORT} 사용 중 오류: {e}")
    print("다른 포트를 사용하거나 실행 중인 서버를 종료해주세요.")