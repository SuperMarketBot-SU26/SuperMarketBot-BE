FROM python:3.10-slim

WORKDIR /app

# Install system packages required by OpenCV
RUN apt-get update && apt-get install -y \
    libgl1 \
    libglib2.0-0 \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Download Haar Cascade XML files for OpenCV face/eye detection since they may be missing in headless packages
RUN python -c "import os, urllib.request, cv2; \
    path = os.path.join(os.path.dirname(cv2.__file__), 'data'); \
    os.makedirs(path, exist_ok=True); \
    urllib.request.urlretrieve('https://raw.githubusercontent.com/opencv/opencv/master/data/haarcascades/haarcascade_frontalface_default.xml', os.path.join(path, 'haarcascade_frontalface_default.xml')); \
    urllib.request.urlretrieve('https://raw.githubusercontent.com/opencv/opencv/master/data/haarcascades/haarcascade_eye.xml', os.path.join(path, 'haarcascade_eye.xml'))"

# Pre-download Facenet model weights to avoid cold start issues during runtime (using detector_backend='skip' to bypass face detection during build)
RUN python -c "from deepface import DeepFace; import numpy as np; DeepFace.represent(img_path=np.zeros((100, 100, 3), dtype=np.uint8), model_name='Facenet', enforce_detection=False, detector_backend='skip')"

COPY . .

EXPOSE 8000

CMD ["python", "app.py"]
