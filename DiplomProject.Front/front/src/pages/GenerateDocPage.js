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
  const canvasRefs = useRef([]);
  const imagesRef = useRef([]);
  const [error, setError] = useState(null);
  const isDrawing = useRef(false);
  const lastPoint = useRef({ x: 0, y: 0 });

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
            file.async('blob').then(imgData => URL.createObjectURL(imgData))
          );
        });
        const imagesArray = await Promise.all(imagesPromises);
        setImages(imagesArray);
        setError(null);
      } catch (err) {
        console.error('Error:', err);
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
    
    const ctx = canvas.getContext('2d');
    canvas.width = img.naturalWidth;
    canvas.height = img.naturalHeight;
    ctx.drawImage(img, 0, 0);
    imagesRef.current[index] = img;
  }, []);

  // Обработчики рисования
  const getCanvasCoordinates = (canvas, e) => {
    const rect = canvas.getBoundingClientRect();
    return {
      x: e.clientX - rect.left,
      y: e.clientY - rect.top
    };
  };

  const startDrawing = useCallback((canvas, ctx, e) => {
    isDrawing.current = true;
    lastPoint.current = getCanvasCoordinates(canvas, e);
    ctx.beginPath();
    ctx.moveTo(lastPoint.current.x, lastPoint.current.y);
  }, []);

  const draw = useCallback((canvas, ctx, e) => {
    if (!isDrawing.current) return;
    
    const newPoint = getCanvasCoordinates(canvas, e);
    
    ctx.globalCompositeOperation = 'destination-out';
    ctx.lineWidth = 20;
    ctx.lineCap = 'round';
    ctx.strokeStyle = 'rgba(0,0,0,1)';
    
    ctx.lineTo(newPoint.x, newPoint.y);
    ctx.stroke();
    
    lastPoint.current = newPoint;
  }, []);

  const stopDrawing = useCallback(() => {
    isDrawing.current = false;
  }, []);

  // Эффект для инструментов
  useEffect(() => {
    const handleMouseDown = (index) => (e) => {
      const canvas = canvasRefs.current[index];
      if (!canvas || selectedTool !== 'eraser') return;
      const ctx = canvas.getContext('2d');
      startDrawing(canvas, ctx, e);
    };

    const handleMouseMove = (index) => (e) => {
      const canvas = canvasRefs.current[index];
      if (!canvas || selectedTool !== 'eraser') return;
      const ctx = canvas.getContext('2d');
      draw(canvas, ctx, e);
    };

    const handleMouseUp = () => {
      stopDrawing();
    };

    const cleanups = canvasRefs.current.map((canvas, index) => {
      if (!canvas) return;

      canvas.addEventListener('mousedown', handleMouseDown(index));
      canvas.addEventListener('mousemove', handleMouseMove(index));
      canvas.addEventListener('mouseup', handleMouseUp);
      canvas.addEventListener('mouseleave', handleMouseUp);

      return () => {
        canvas.removeEventListener('mousedown', handleMouseDown(index));
        canvas.removeEventListener('mousemove', handleMouseMove(index));
        canvas.removeEventListener('mouseup', handleMouseUp);
        canvas.removeEventListener('mouseleave', handleMouseUp);
      };
    });

    return () => cleanups.forEach(cleanup => cleanup && cleanup());
  }, [selectedTool, startDrawing, draw, stopDrawing]);

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
          <div key={index} className="image-wrapper">
            <canvas
              ref={el => canvasRefs.current[index] = el}
              className="image-canvas"
              style={{ cursor: selectedTool === 'eraser' ? 'crosshair' : 'default' }}
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