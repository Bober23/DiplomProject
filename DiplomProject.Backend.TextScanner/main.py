from fastapi import FastAPI, Body, HTTPException
from easyocr import Reader
from PIL import Image
import io
import logging
from typing import List
import numpy as np

app = FastAPI()
logger = logging.getLogger("uvicorn")

# Инициализация EasyOCR
reader = Reader(['ru', 'en'], gpu=True)  # Для GPU измените на True

def preprocess_image(image: Image.Image) -> np.ndarray:
    """Предобработка изображения для EasyOCR"""
    # Конвертация в RGB
    if image.mode != 'RGB':
        image = image.convert('RGB')
    
    # Увеличение разрешения для мелкого текста
    if max(image.size) < 1024:
        new_size = (image.width * 2, image.height * 2)
        image = image.resize(new_size, Image.Resampling.LANCZOS)
    
    return np.array(image)

@app.post("/recognize-text/")
async def recognize_text(file: bytes = Body(...)):  # Принимаем сырые байты
        # Преобразование байтов в изображение
    try:
        # Преобразование байтов в изображение
        image = Image.open(io.BytesIO(file))
        img_array = preprocess_image(image)

        # Распознавание текста
        result = reader.readtext(img_array, detail=0)
        combined_text = " ".join(result)
            
    except Exception as e:
        logger.error(f"Error processing {image.filename}: {str(e)}")
    
    return combined_text

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=8000)