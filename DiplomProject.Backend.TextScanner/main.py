from fastapi import FastAPI, File, UploadFile, HTTPException
from easyocr import Reader
from PIL import Image
import io
import logging
from typing import List
import numpy as np

app = FastAPI()
logger = logging.getLogger("uvicorn")

# Инициализация EasyOCR
reader = Reader(['ru', 'en'], gpu=False)  # Для GPU измените на True

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
async def recognize_text(files: List[UploadFile] = File(...)):
    results = []
    
    for file in files:
        try:
            # Валидация файла
            if not file.content_type.startswith("image/"):
                raise HTTPException(400, "Invalid file type")

            # Чтение и обработка изображения
            image_data = await file.read()
            image = Image.open(io.BytesIO(image_data))
            img_array = preprocess_image(image)
            
            # Распознавание текста
            result = reader.readtext(img_array, detail=0)  # detail=0 возвращает только текст
            combined_text = " ".join(result)
            
            results.append({
                "filename": file.filename,
                "text": combined_text,
                "status": "success"
            })
            
        except Exception as e:
            logger.error(f"Error processing {file.filename}: {str(e)}")
            results.append({
                "filename": file.filename,
                "text": "",
                "status": "error",
                "message": str(e)
            })
    
    return {"results": results}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)