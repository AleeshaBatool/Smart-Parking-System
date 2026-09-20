from flask import Flask, request, jsonify, send_file
import os
import random
from datetime import datetime

app = Flask(__name__)

# Folder to save images
UPLOAD_FOLDER = 'received_images'
os.makedirs(UPLOAD_FOLDER, exist_ok=True)

@app.route('/upload', methods=['POST'])
def upload_image():
    if 'image' not in request.files:
        return jsonify({'error': 'No image part in the request'}), 400

    file = request.files['image']
    if file.filename == '':
        return jsonify({'error': 'No selected image'}), 400

    # Save image with a timestamped filename
    filename = datetime.now().strftime("%Y%m%d_%H%M%S") + ".jpg"
    filepath = os.path.join(UPLOAD_FOLDER, filename)
    file.save(filepath)

    print(f"[INFO] Image received and saved as {filename}")

    # You can display the image in any GUI or just confirm it's saved
    # Simulating model prediction
    label = random.choice(["car", "bike"])

    return jsonify({'label': label})


# Optional: To view the latest image from browser
@app.route('/latest-image', methods=['GET'])
def view_latest_image():
    files = sorted(os.listdir(UPLOAD_FOLDER), reverse=True)
    if not files:
        return "No images received yet.", 404

    latest_image_path = os.path.join(UPLOAD_FOLDER, files[0])
    return send_file(latest_image_path, mimetype='image/jpeg')

if __name__ == '__main__':
    # Replace host='0.0.0.0' if deploying to your LAN
    app.run(host='0.0.0.0', port=5000)
