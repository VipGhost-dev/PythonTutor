import socket
import json
import sys
import traceback
import base64

def execute_code(code):
    """Выполнение Python кода игрока"""
    output_lines = []
    
    def custom_print(*args):
        msg = ' '.join(str(arg) for arg in args)
        output_lines.append(msg)
        print(f"[Player] {msg}")
    
    try:
        # Создаём API для игрока
        api = GameAPI()
        
        # Создаём безопасное окружение
        safe_globals = {
            '__builtins__': {
                'print': custom_print,
                'range': range,
                'len': len,
                'str': str,
                'int': int,
                'float': float,
                'list': list,
                'dict': dict,
                'tuple': tuple,
                'True': True,
                'False': False,
                'None': None,
            },
            'api': api
        }
        
        # Выполняем код
        exec(code, safe_globals)
        
        output = '\n'.join(output_lines) if output_lines else 'Code executed successfully!'
        
        return {
            'success': True,
            'output': output
        }
        
    except Exception as e:
        error_msg = f"{type(e).__name__}: {str(e)}"
        print(f"Error executing code: {error_msg}")
        traceback.print_exc()
        
        output = '\n'.join(output_lines) if output_lines else ''
        
        return {
            'success': False,
            'error': error_msg,
            'traceback': traceback.format_exc(),
            'output': output
        }


class GameAPI:
    """API для взаимодействия с игрой"""
    
    def move(self, direction):
        valid_directions = ['up', 'down', 'left', 'right']
        if direction not in valid_directions:
            return f"ERROR: Invalid direction. Use: {valid_directions}"
        
        print(f"🤖 Moving {direction}")
        return "OK"
    
    def harvest(self):
        print(f"🌾 Harvesting at current position")
        return 10
    
    def plant(self, seed_type):
        valid_seeds = ['wheat', 'corn', 'carrot']
        if seed_type not in valid_seeds:
            return f"ERROR: Invalid seed. Use: {valid_seeds}"
        
        print(f"🌱 Planting {seed_type}")
        return True
    
    def get_position(self):
        return (5, 5)
    
    def scan(self, radius=1):
        return {
            "center": (5, 5),
            "radius": radius,
            "cells": []
        }
    
    def get_coins(self):
        return 100
    
    def wait(self, seconds):
        import time
        time.sleep(seconds)
        return f"Waited {seconds} seconds"


def handle_client(client_socket, addr):
    """Обработка одного клиента"""
    try:
        # Получаем данные
        data = client_socket.recv(65536)
        if not data:
            return
        
        data_str = data.decode('utf-8')
        print(f"\n📥 Received from {addr}")
        
        # Парсим JSON
        try:
            request = json.loads(data_str)
        except json.JSONDecodeError as e:
            print(f"❌ JSON decode error: {e}")
            error_response = {'success': False, 'error': f'Invalid JSON: {e}'}
            client_socket.send(json.dumps(error_response).encode('utf-8'))
            return
        
        command = request.get('command', '')
        encoded_data = request.get('data', '')
        
        print(f"📋 Command: '{command}'")
        
        # Обрабатываем команду
        if command == 'execute':
            # Декодируем Base64
            try:
                code = base64.b64decode(encoded_data).decode('utf-8')
                print(f"📝 Decoded code ({len(code)} chars)")
                print(f"   Preview: {code[:100]}...")
                response = execute_code(code)
            except Exception as e:
                print(f"❌ Base64 decode error: {e}")
                response = {'success': False, 'error': f'Failed to decode code: {e}'}
                
        elif command == 'test':
            response = {'success': True, 'message': 'Server is running!'}
        else:
            response = {'success': False, 'error': f'Unknown command: {command}'}
        
        # Отправляем ответ
        response_json = json.dumps(response)
        client_socket.send(response_json.encode('utf-8'))
        print(f"📤 Sent response: success={response.get('success', False)}")
        
    except Exception as e:
        print(f"❌ Error in handle_client: {e}")
        traceback.print_exc()
    finally:
        client_socket.close()
        print(f"🔌 Connection closed")


def start_server(host='localhost', port=9999):
    """Запуск сервера"""
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server_socket.bind((host, port))
    server_socket.listen(5)
    
    print("=" * 50)
    print("🐍 FARMING SERVER FOR ROBOFARMER")
    print("=" * 50)
    print(f"🚀 Server started on {host}:{port}")
    print("Waiting for Unity client...")
    print("Press Ctrl+C to stop the server")
    print("-" * 50)
    
    try:
        while True:
            client_socket, addr = server_socket.accept()
            print(f"\n📡 New connection from {addr}")
            handle_client(client_socket, addr)
    except KeyboardInterrupt:
        print("\n\n🛑 Stopping server...")
    finally:
        server_socket.close()
        print("Server stopped")


if __name__ == "__main__":
    start_server()