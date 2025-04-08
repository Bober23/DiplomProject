import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import { Button, Spin, message } from 'antd';
import JSZip from 'jszip';
import './GenerateDocPage.css';

const GenerateDocPage = () => {
  const { documentId } = useParams();
  const [images, setImages] = useState([]);
  const [selectedTool, setSelectedTool] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  
  const canvasRefs = useRef([]);
  const imagesRef = useRef([]);
  const selectionState = useRef({
    startX: 0,
    startY: 0,
    isSelecting: false,
    tempCanvas: null
  });

  // Загрузка изображений
  useEffect(() => {
    const loadImages = async () => {
      try {
        const response = await fetch(`http://localhost:5120/api/Document/images/${documentId}`);
        if (!response.ok) throw new Error('Ошибка загрузки архива');
        
        const zipData = await response.blob();
        const zip = await JSZip.loadAsync(zipData);
        
        const imagesPromises = [];
        zip.forEach((relativePath, file) => {
          imagesPromises.push(
            file.async('blob').then(imgData => URL.createObjectURL(imgData)))
        });

        const imagesArray = await Promise.all(imagesPromises);
        setImages(imagesArray);
        setError(null);
      } catch (err) {
        message.error('Не удалось загрузить изображения');
        setError(err.message);
      } finally {
        setLoading(false);
      }
    };

    loadImages();
  }, [documentId]);

  // Инициализация canvas
  const initializeCanvas = useCallback((img, index) => {
    const canvas = canvasRefs.current[index];
    if (!canvas) return;

    const parentWidth = canvas.parentElement.offsetWidth;
    const scale = parentWidth / img.naturalWidth;
    
    canvas.width = img.naturalWidth;
    canvas.height = img.naturalHeight;
    canvas.style.width = `${parentWidth}px`;
    canvas.style.height = `${img.naturalHeight * scale}px`;

    const ctx = canvas.getContext('2d');
    ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
    imagesRef.current[index] = img;
  }, []);

  // Получение координат с учетом масштаба
  const getCanvasCoordinates = useCallback((canvas, e) => {
    const rect = canvas.getBoundingClientRect();
    const scaleX = canvas.width / rect.width;
    const scaleY = canvas.height / rect.height;
    return {
      x: (e.clientX - rect.left) * scaleX,
      y: (e.clientY - rect.top) * scaleY
    };
  }, []);

  // Обработчики инструментов
  const setupEraser = useCallback((canvas) => {
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    let isDrawing = false;

    const handleMouseDown = (e) => {
      isDrawing = true;
      const coords = getCanvasCoordinates(canvas, e);
      ctx.beginPath();
      ctx.moveTo(coords.x, coords.y);
    };

    const handleMouseMove = (e) => {
      if (!isDrawing) return;
      const coords = getCanvasCoordinates(canvas, e);
      
      ctx.globalCompositeOperation = 'destination-out';
      ctx.lineWidth = 20;
      ctx.lineCap = 'round';
      
      ctx.lineTo(coords.x, coords.y);
      ctx.stroke();
    };

    const handleMouseUp = () => {
      isDrawing = false;
      ctx.closePath();
    };

    canvas.addEventListener('mousedown', handleMouseDown);
    canvas.addEventListener('mousemove', handleMouseMove);
    canvas.addEventListener('mouseup', handleMouseUp);
    canvas.addEventListener('mouseleave', handleMouseUp);

    return () => {
      canvas.removeEventListener('mousedown', handleMouseDown);
      canvas.removeEventListener('mousemove', handleMouseMove);
      canvas.removeEventListener('mouseup', handleMouseUp);
      canvas.removeEventListener('mouseleave', handleMouseUp);
    };
  }, [getCanvasCoordinates]);

  const setupSelection = useCallback((canvas) => {
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    
    // Создаем временный canvas для сохранения состояния
    const tempCanvas = document.createElement('canvas');
    tempCanvas.width = canvas.width;
    tempCanvas.height = canvas.height;
    const tempCtx = tempCanvas.getContext('2d');
    tempCtx.drawImage(canvas, 0, 0);
    selectionState.current.tempCanvas = tempCanvas;

    const handleMouseDown = (e) => {
      selectionState.current.isSelecting = true;
      const coords = getCanvasCoordinates(canvas, e);
      selectionState.current.startX = coords.x;
      selectionState.current.startY = coords.y;
    };

    const handleMouseMove = (e) => {
      if (!selectionState.current.isSelecting) return;
      
      const coords = getCanvasCoordinates(canvas, e);
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      ctx.drawImage(tempCanvas, 0, 0);
      
      ctx.strokeStyle = 'red';
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.rect(
        selectionState.current.startX,
        selectionState.current.startY,
        coords.x - selectionState.current.startX,
        coords.y - selectionState.current.startY
      );
      ctx.stroke();
    };

    const handleMouseUp = () => {
      selectionState.current.isSelecting = false;
      tempCtx.drawImage(canvas, 0, 0);
    };

    canvas.addEventListener('mousedown', handleMouseDown);
    canvas.addEventListener('mousemove', handleMouseMove);
    canvas.addEventListener('mouseup', handleMouseUp);
    canvas.addEventListener('mouseleave', handleMouseUp);

    return () => {
      canvas.removeEventListener('mousedown', handleMouseDown);
      canvas.removeEventListener('mousemove', handleMouseMove);
      canvas.removeEventListener('mouseup', handleMouseUp);
      canvas.removeEventListener('mouseleave', handleMouseUp);
    };
  }, [getCanvasCoordinates]);

  // Управление инструментами
  useEffect(() => {
    const cleanups = canvasRefs.current.map((canvas, index) => {
      if (!canvas || !imagesRef.current[index]) return;

      // Клонируем canvas для очистки обработчиков
      const newCanvas = canvas.cloneNode(true);
      canvas.parentNode.replaceChild(newCanvas, canvas);
      canvasRefs.current[index] = newCanvas;

      // Восстанавливаем изображение
      const ctx = newCanvas.getContext('2d');
      ctx.drawImage(imagesRef.current[index], 0, 0);

      // Устанавливаем новые обработчики
      if (selectedTool === 'eraser') {
        return setupEraser(newCanvas);
      }
      if (selectedTool === 'selection') {
        return setupSelection(newCanvas);
      }
    });

    return () => cleanups.forEach(cleanup => cleanup?.());
  }, [selectedTool, setupEraser, setupSelection]);

  if (error) return <div className="error-message">Ошибка: {error}</div>;
  
  if (loading) {
    return (
      <div className="loading-container">
        <Spin size="large" tip="Загрузка изображений..." />
      </div>
    );
  }

  return (
    <div className="generate-doc-page">
      <div className="toolbar">
        <Button
          type={selectedTool === 'eraser' ? 'primary' : 'default'}
          onClick={() => setSelectedTool('eraser')}
          style={{ backgroundColor: selectedTool === 'eraser' ? '#2383E2' : '' }}
        >
          Ластик
        </Button>
        <Button
          type={selectedTool === 'selection' ? 'primary' : 'default'}
          onClick={() => setSelectedTool('selection')}
          style={{ backgroundColor: selectedTool === 'selection' ? '#2383E2' : '' }}
        >
          Выделение
        </Button>
      </div>

      <div className="images-container">
        {images.map((imgSrc, index) => (
          <div key={index} className="image-wrapper" style={{ width: '90%' }}>
            <canvas
              ref={el => canvasRefs.current[index] = el}
              className="image-canvas"
              style={{ 
                cursor: selectedTool === 'eraser' ? 'crosshair' 
                  : selectedTool === 'selection' ? 'cell' : 'default' 
              }}
            />
            <img 
              src={imgSrc} 
              alt={`Document ${index}`}
              onLoad={(e) => initializeCanvas(e.target, index)}
              style={{ display: 'none' }}
            />
          </div>
        ))}
      </div>
    </div>
  );
};

export default GenerateDocPage;