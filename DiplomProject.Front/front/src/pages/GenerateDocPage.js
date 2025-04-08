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
  const buffersRef = useRef([]);
  const imagesRef = useRef([]);

  // Загрузка изображений
  useEffect(() => {
    const loadImages = async () => {
      try {
        const response = await fetch(`http://localhost:5120/api/Document/images/${documentId}`);
        if (!response.ok) throw new Error('Ошибка загрузки архива');
        
        const zipData = await response.blob();
        const zip = await JSZip.loadAsync(zipData);
        
        const imagesArray = await Promise.all(
          Object.values(zip.files).map(file => 
            file.async('blob').then(imgData => URL.createObjectURL(imgData)))
        );
        
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

    // Инициализация буферов
    buffersRef.current[index] = {
      main: document.createElement('canvas'),
      temp: document.createElement('canvas')
    };
    
    const { main, temp } = buffersRef.current[index];
    main.width = canvas.width;
    main.height = canvas.height;
    temp.width = canvas.width;
    temp.height = canvas.height;

    const mainCtx = main.getContext('2d');
    mainCtx.drawImage(img, 0, 0);
    
    const ctx = canvas.getContext('2d');
    ctx.drawImage(main, 0, 0);
    imagesRef.current[index] = img;
  }, []);

  // Получение координат
  const getCanvasCoordinates = useCallback((canvas, e) => {
    const rect = canvas.getBoundingClientRect();
    return {
      x: (e.clientX - rect.left) * (canvas.width / rect.width),
      y: (e.clientY - rect.top) * (canvas.height / rect.height)
    };
  }, []);

  // Обработчик ластика
  const setupEraser = useCallback((index) => {
    const canvas = canvasRefs.current[index];
    const buffer = buffersRef.current[index];
    if (!canvas || !buffer) return;

    const ctx = canvas.getContext('2d');
    const mainCtx = buffer.main.getContext('2d');
    let isDrawing = false;
    let lastPoint = null;

    const startDrawing = (e) => {
      isDrawing = true;
      lastPoint = getCanvasCoordinates(canvas, e);
      ctx.beginPath();
      ctx.moveTo(lastPoint.x, lastPoint.y);
      ctx.globalCompositeOperation = 'destination-out';
      ctx.lineWidth = 20;
      ctx.lineCap = 'round';
    };

    const draw = (e) => {
      if (!isDrawing) return;
      const newPoint = getCanvasCoordinates(canvas, e);
      
      ctx.lineTo(newPoint.x, newPoint.y);
      ctx.stroke();
      
      // Рисуем в основной буфер
      mainCtx.globalCompositeOperation = 'destination-out';
      mainCtx.lineWidth = 20;
      mainCtx.lineCap = 'round';
      mainCtx.beginPath();
      mainCtx.moveTo(lastPoint.x, lastPoint.y);
      mainCtx.lineTo(newPoint.x, newPoint.y);
      mainCtx.stroke();

      lastPoint = newPoint;
    };

    const endDrawing = () => {
      isDrawing = false;
      ctx.closePath();
    };

    canvas.addEventListener('mousedown', startDrawing);
    canvas.addEventListener('mousemove', draw);
    canvas.addEventListener('mouseup', endDrawing);
    canvas.addEventListener('mouseleave', endDrawing);

    return () => {
      canvas.removeEventListener('mousedown', startDrawing);
      canvas.removeEventListener('mousemove', draw);
      canvas.removeEventListener('mouseup', endDrawing);
      canvas.removeEventListener('mouseleave', endDrawing);
    };
  }, [getCanvasCoordinates]);

  // Обработчик выделения
  const setupSelection = useCallback((index) => {
    const canvas = canvasRefs.current[index];
    const buffer = buffersRef.current[index];
    if (!canvas || !buffer) return;

    const ctx = canvas.getContext('2d');
    const mainCtx = buffer.main.getContext('2d');
    let isSelecting = false;
    let startX = 0, startY = 0;

    const startSelection = (e) => {
      isSelecting = true;
      const coords = getCanvasCoordinates(canvas, e);
      startX = coords.x;
      startY = coords.y;
    };

    const drawSelection = (e) => {
      if (!isSelecting) return;
      const coords = getCanvasCoordinates(canvas, e);
      
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      ctx.drawImage(buffer.main, 0, 0);
      
      ctx.strokeStyle = 'red';
      ctx.lineWidth = 2;
      ctx.strokeRect(startX, startY, coords.x - startX, coords.y - startY);
    };

    const endSelection = () => {
      isSelecting = false;
      mainCtx.drawImage(canvas, 0, 0);
    };

    canvas.addEventListener('mousedown', startSelection);
    canvas.addEventListener('mousemove', drawSelection);
    canvas.addEventListener('mouseup', endSelection);
    canvas.addEventListener('mouseleave', endSelection);

    return () => {
      canvas.removeEventListener('mousedown', startSelection);
      canvas.removeEventListener('mousemove', drawSelection);
      canvas.removeEventListener('mouseup', endSelection);
      canvas.removeEventListener('mouseleave', endSelection);
    };
  }, [getCanvasCoordinates]);

  // Эффект переключения инструментов
  useEffect(() => {
    const cleanups = canvasRefs.current.map((canvas, index) => {
      if (!canvas || !buffersRef.current[index]) return;
      
      const ctx = canvas.getContext('2d');
      ctx.drawImage(buffersRef.current[index].main, 0, 0);

      return selectedTool === 'eraser' 
        ? setupEraser(index)
        : selectedTool === 'selection'
        ? setupSelection(index)
        : null;
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
                  : selectedTool === 'selection' ? 'cell' : 'default',
                background: 'white'
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