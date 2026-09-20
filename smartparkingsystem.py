from flask import Flask, request, jsonify, send_file
import os
import random
from datetime import datetime

app = Flask(__name__)

UPLOAD_FOLDER = 'received_images'
os.makedirs(UPLOAD_FOLDER, exist_ok=True)

@app.route('/upload', methods=['POST'])
def upload_image():
    if 'image' not in request.files:
        return jsonify({'error': 'No image part in the request'}), 400

    file = request.files['image']
    data = file.read()

    if not data:
        return jsonify({'error': 'Empty file'}), 400

    filename = datetime.now().strftime("%Y%m%d_%H%M%S") + ".jpg"
    filepath = os.path.join(UPLOAD_FOLDER, filename)

    # Save manually to ensure binary correctness
    with open(filepath, 'wb') as f:
        f.write(data)

    # Debug info
    print(f"[INFO] Image saved: {filename} ({len(data)} bytes)")
    print(f"First 2 bytes: {data[:2].hex()}")
    print(f"Last 2 bytes: {data[-2:].hex()}")

    label = random.choice(["car", "bike"])

    return jsonify({'label': label})

@app.route('/latest-image', methods=['GET'])
def view_latest_image():
    files = sorted(os.listdir(UPLOAD_FOLDER), reverse=True)
    if not files:
        return "No images received yet.", 404

    latest_image_path = os.path.join(UPLOAD_FOLDER, files[0])
    return send_file(latest_image_path, mimetype='image/jpeg')

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)
