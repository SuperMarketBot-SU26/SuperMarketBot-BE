FROM python:3.10-slim

WORKDIR /app

# Install system packages required by OpenCV
RUN apt-get update && apt-get install -y \
    libgl1 \
    libglib2.0-0 \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Pre-download Facenet model weights to avoid cold start issues during runtime
RUN python -c "from deepface import DeepFace; import numpy as np; DeepFace.represent(img_path=np.zeros((100, 100, 3), dtype=np.uint8), model_name='Facenet', enforce_detection=False)"

COPY . .

EXPOSE 8000

CMD ["python", "app.py"]
