import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import { Button, Spin, message } from 'antd';
import JSZip from 'jszip';
import './GenerateDocPage.css';

const MAX_HISTORY_STEPS = 100;

const GenerateDocPage = () => {
  const { documentId } = useParams();
  const [images, setImages] = useState([]);
  const [selectedTool, setSelectedTool] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [selections, setSelections] = useState([]);

  const selectionsRef = useRef([]);
  const canvasRefs = useRef([]);
  const buffersRef = useRef([]);
  const imagesRef = useRef([]);
  const history = useRef([]);
  const historyPosition = useRef(-1);

  useEffect(() => {
    selectionsRef.current = selections;
  }, [selections]);

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
            file.async('blob').then(imgData => URL.createObjectURL(imgData))
          )
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

  const handleGenerate = async () => {
    try {
      // Сбор изображений
      const blobs = await Promise.all(
        canvasRefs.current.map((canvas, index) => {
          if (!canvas) return Promise.resolve(null);
          return new Promise((resolve) => {
            canvas.toBlob((blob) => {
              resolve(blob ? { blob, index } : null);
            }, 'image/png');
          });
        })
      );
  
      // Создание ZIP
      const zip = new JSZip();
      blobs.forEach(blobInfo => {
        if (blobInfo?.blob) {
          zip.file(`image_${blobInfo.index}.png`, blobInfo.blob);
        }
      });
  
      // Сбор выделений
      const selectionsData = [];
      selectionsRef.current.forEach((imageSelections, index) => {
        imageSelections?.forEach(selection => {
          selectionsData.push({
            id: index.toString(),
            points: selection.points
          });
        });
      });
  
      // Формирование запроса
      const formData = new FormData();
      const zipBlob = await zip.generateAsync({ type: 'blob' });
      formData.append('images', zipBlob, 'images.zip');
      formData.append('selections', JSON.stringify(selectionsData));
  
      // Отправка
      const response = await fetch(
        `http://localhost:5120/api/Document/generate/${documentId}`,
        { method: 'POST', body: formData }
      );
  
      if (!response.ok) throw new Error('Ошибка отправки');
      message.success('Данные отправлены!');
    } catch (error) {
      message.error('Ошибка: ' + error.message);
    }
  };

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

    // Сохраняем начальное состояние
    saveHistoryState(index);
  }, []);

  // Сохранение состояния в историю
  const saveHistoryState = (index) => {
    const buffer = buffersRef.current[index];
    if (!buffer) return;
    const stateCanvas = document.createElement('canvas');
    stateCanvas.width = buffer.main.width;
    stateCanvas.height = buffer.main.height;
    const stateCtx = stateCanvas.getContext('2d');
    stateCtx.drawImage(buffer.main, 0, 0);

    const newEntry = {
      index,
      state: stateCanvas,
      selections: [...(selectionsRef.current[index] || [])]
    };

    // Если текущая позиция не в конце истории - обрезаем
    if (historyPosition.current < history.current.length - 1) {
      history.current = history.current.slice(0, historyPosition.current + 1);
    }

    history.current.push(newEntry);
    historyPosition.current = history.current.length - 1;

    // Ограничиваем размер истории
    if (history.current.length > MAX_HISTORY_STEPS) {
      history.current.shift();
      historyPosition.current--;
    }
  };

  // Отмена действия
  const undo = () => {
    if (historyPosition.current < 0) return;
    const newSelections = [...selectionsRef.current];
    newSelections[prevEntry.index] = prevEntry.selections;
    setSelections(newSelections);
    
    const currentEntry = history.current[historyPosition.current];
    const prevEntry = history.current[historyPosition.current - 1];

    // Восстанавливаем предыдущее состояние
    if (prevEntry) {
      const buffer = buffersRef.current[prevEntry.index];
      const canvas = canvasRefs.current[prevEntry.index];

      if (buffer && canvas) {
        buffer.main.getContext('2d').drawImage(prevEntry.state, 0, 0);
        canvas.getContext('2d').drawImage(prevEntry.state, 0, 0);
      }
    }

    historyPosition.current--;
  };

  // Повтор действия
  const redo = () => {
    if (historyPosition.current >= history.current.length - 1) return;

    historyPosition.current++;
    const entry = history.current[historyPosition.current];

    const buffer = buffersRef.current[entry.index];
    const canvas = canvasRefs.current[entry.index];

    if (buffer && canvas) {
      buffer.main.getContext('2d').drawImage(entry.state, 0, 0);
      canvas.getContext('2d').drawImage(entry.state, 0, 0);
    }
  };

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
      saveHistoryState(index);
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
      saveHistoryState(index);
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
      saveHistoryState(index);
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

      // Рассчитываем границы прямоугольника
      const x1 = Math.min(startX, coords.x);
      const y1 = Math.min(startY, coords.y);
      const x2 = Math.max(startX, coords.x);
      const y2 = Math.max(startY, coords.y);
      const canvasWidth = canvas.width;
      const canvasHeight = canvas.height;

      ctx.save();

      // Рисуем верхнюю, левую и правую стороны
      ctx.beginPath();
      // Верхняя линия
      ctx.moveTo(x1, y1);
      ctx.lineTo(x2, y1);
      // Левая линия
      ctx.moveTo(x1, y1);
      ctx.lineTo(x1, y2);
      // Правая линия
      ctx.moveTo(x2, y1);
      ctx.lineTo(x2, y2);
      ctx.stroke();

      // Рисуем нижнюю линию с пунктирными продолжениями
      ctx.beginPath();
      ctx.setLineDash([5, 5]); // Настройки пунктира

      // Левая пунктирная часть
      if (x1 > 0) {
        ctx.moveTo(0, y2);
        ctx.lineTo(x1, y2);
      }

      // Средняя сплошная часть
      ctx.setLineDash([]);
      ctx.moveTo(x1, y2);
      ctx.lineTo(x2, y2);

      // Правая пунктирная часть
      if (x2 < canvasWidth) {
        ctx.setLineDash([5, 5]);
        ctx.moveTo(x2, y2);
        ctx.lineTo(canvasWidth, y2);
      }

      ctx.stroke();

      ctx.restore(); // Восстанавливаем настройки контекста
    };

    const endSelection = (e) => {
      if (!isSelecting) return;
      isSelecting = false;
    
      if (!e) {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        ctx.drawImage(buffer.main, 0, 0);
        return;
      }
    
      const coords = getCanvasCoordinates(canvas, e);
      const currentX = coords.x;
      const currentY = coords.y;
    
      const x1 = Math.min(startX, currentX);
      const y1 = Math.min(startY, currentY);
      const x2 = Math.max(startX, currentX);
      const y2 = Math.max(startY, currentY);
    
      const newSelection = {
        points: [
          { x: x1, y: y1 },
          { x: x2, y: y1 },
          { x: x1, y: y2 },
          { x: x2, y: y2 },
        ]
      };
    
      setSelections(prev => {
        const newSelections = [...prev];
        if (!newSelections[index]) newSelections[index] = [];
        newSelections[index].push(newSelection);
        return newSelections;
      });
    
      mainCtx.drawImage(canvas, 0, 0);
      saveHistoryState(index);
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

        <div className="undo-redo-container">
          <Button
            onClick={undo}
            disabled={historyPosition.current < 0}
          >
            Undo
          </Button>
        </div>
        <Button
          type="primary"
          onClick={handleGenerate}
          style={{ backgroundColor: '#2383E2', marginLeft: 'auto' }}
        >
          Сгенерировать
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