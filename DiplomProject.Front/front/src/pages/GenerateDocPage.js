import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import { Button, Row, Col, Space, Spin, message, Typography } from 'antd';
import { DeleteOutlined, HighlightOutlined } from '@ant-design/icons';
import { Stage, Layer, Image as KonvaImage, Line, Rect } from 'react-konva';
import styled from 'styled-components';
import { useAuth } from '../components/AuthContext';
import JSZip from 'jszip';

const { Title, Text } = Typography;

const GenerateDocPage = () => {
  const { documentId } = useParams();
  const { state } = useLocation();
  const { user } = useAuth();
  const navigate = useNavigate();
  
  const [tool, setTool] = useState('eraser');
  const [images, setImages] = useState(state?.images || []);
  const [modifications, setModifications] = useState({});
  const [loading, setLoading] = useState(!state?.images);
  const [error, setError] = useState(null);

  const processZipArchive = async (zipData) => {
    const zip = await JSZip.loadAsync(zipData);
    const files = Object.values(zip.files).filter(file => !file.dir);
    
    const processedImages = await Promise.all(
      files.map(async (zipEntry) => {
        try {
          const fileData = await zipEntry.async('arraybuffer');
          const blob = new Blob([fileData], { type: `image/${getFileType(zipEntry.name)}` });
          return {
            id: zipEntry.name,
            name: zipEntry.name.split('/').pop(),
            blob: blob,
            size: zipEntry._data.uncompressedSize,
            url: URL.createObjectURL(blob)
          };
        } catch (error) {
          console.error('Ошибка обработки файла:', zipEntry.name, error);
          return null;
        }
      })
    );
    
    return processedImages
      .filter(img => img !== null)
      .sort((a, b) => a.name.localeCompare(b.name));
  };

  const getFileType = (filename) => {
    const ext = filename.split('.').pop().toLowerCase();
    return ext === 'jpg' ? 'jpeg' : ext;
  };

  useEffect(() => {
    const abortController = new AbortController();

    const fetchData = async () => {
      if (!state?.images) {
        try {
          setLoading(true);
          const response = await fetch(
            `http://localhost:5120/api/Document/images/${documentId}`,
            {
              headers: {
                'Authorization': `Bearer ${user.token}`
              },
              signal: abortController.signal
            }
          );

          if (!response.ok) throw new Error('Ошибка загрузки изображений');
          const zipData = await response.blob();
          const processedImages = await processZipArchive(zipData);
          setImages(processedImages);
        } catch (err) {
          if (!abortController.signal.aborted) {
            setError(err.message);
          }
        } finally {
          if (!abortController.signal.aborted) {
            setLoading(false);
          }
        }
      }
    };

    fetchData();
    return () => abortController.abort();
  }, [documentId, user.token, state]);

  const handleImageEdit = (imageId, newModifications) => {
    setModifications(prev => ({
      ...prev,
      [imageId]: [...(prev[imageId] || []), newModifications]
    }));
  };

  const handleGenerate = () => {
    message.success('Изменения сохранены. Генерация документа...');
    navigate(-1);
  };

  if (error) {
    return (
      <Container>
        <ErrorContainer>
          <Text type="danger" style={{ fontSize: 16 }}>{error}</Text>
          <Button 
            type="primary" 
            style={{ marginTop: 16 }}
            onClick={() => navigate(-1)}
          >
            Назад
          </Button>
        </ErrorContainer>
      </Container>
    );
  }

  if (loading) {
    return <Spin size="large" fullscreen />;
  }

  return (
    <Container>
      <Toolbar>
        <Space>
          <Button
            type={tool === 'eraser' ? 'primary' : 'default'}
            icon={<DeleteOutlined />}
            onClick={() => setTool('eraser')}
          >
            Ластик
          </Button>
          <Button
            type={tool === 'selection' ? 'primary' : 'default'}
            icon={<HighlightOutlined />}
            onClick={() => setTool('selection')}
          >
            Выделение
          </Button>
          <Button type="primary" onClick={handleGenerate}>
            Сгенерировать документ
          </Button>
        </Space>
      </Toolbar>

      <ImageGrid>
        <Row gutter={[16, 16]}>
          {images.map((image) => (
            <Col key={image.id} xs={24} md={12} lg={8}>
              <ImageEditor 
                image={image}
                tool={tool}
                modifications={modifications[image.id] || []}
                onModify={handleImageEdit}
              />
            </Col>
          ))}
        </Row>
      </ImageGrid>
    </Container>
  );
};

const ImageEditor = ({ image, tool, modifications, onModify }) => {
    const [size, setSize] = useState({ width: 0, height: 0 });
    const [isDrawing, setIsDrawing] = useState(false);
    const [startPoint, setStartPoint] = useState(null);
    const [currentShape, setCurrentShape] = useState(null);
    const imgRef = useRef(new window.Image());
    const [imgUrl, setImgUrl] = useState('');
  
    useEffect(() => {
      const initializeImage = async () => {
        try {
          // Если есть оригинальный Blob
          if (image.blob instanceof Blob) {
            const reader = new FileReader();
            reader.onload = (e) => {
              imgRef.current.src = e.target.result;
              imgRef.current.onload = () => {
                setSize({
                  width: imgRef.current.naturalWidth,
                  height: imgRef.current.naturalHeight
                });
                setImgUrl(e.target.result);
              };
            };
            reader.readAsDataURL(image.blob);
          }
          // Если изображение пришло через state (только URL)
          else if (image.url) {
            const response = await fetch(image.url);
            const blob = await response.blob();
            const reader = new FileReader();
            reader.onload = (e) => {
              imgRef.current.src = e.target.result;
              imgRef.current.onload = () => {
                setSize({
                  width: imgRef.current.naturalWidth,
                  height: imgRef.current.naturalHeight
                });
                setImgUrl(e.target.result);
              };
            };
            reader.readAsDataURL(blob);
          }
        } catch (error) {
          console.error('Ошибка загрузки изображения:', error);
        }
      };
  
      initializeImage();
  
      return () => {
        if (imgUrl) URL.revokeObjectURL(imgUrl);
      };
    }, [image]);

  const handleMouseDown = (e) => {
    const stage = e.target.getStage();
    const pos = stage.getPointerPosition();
    setIsDrawing(true);
    setStartPoint(pos);
    
    if (tool === 'selection') {
      setCurrentShape({
        type: 'rect',
        x: pos.x,
        y: pos.y,
        width: 0,
        height: 0,
        stroke: 'red',
        strokeWidth: 2
      });
    }
  };

  const handleMouseMove = (e) => {
    if (!isDrawing || !startPoint) return;

    const stage = e.target.getStage();
    const pos = stage.getPointerPosition();
    
    if (tool === 'eraser') {
      const line = {
        type: 'line',
        points: [startPoint.x, startPoint.y, pos.x, pos.y],
        stroke: 'white',
        strokeWidth: 20,
        lineCap: 'round',
        lineJoin: 'round'
      };
      onModify(image.id, line);
      setStartPoint(pos);
    }

    if (tool === 'selection' && currentShape) {
      setCurrentShape({
        ...currentShape,
        width: pos.x - startPoint.x,
        height: pos.y - startPoint.y
      });
    }
  };

  const handleMouseUp = () => {
    if (tool === 'selection' && currentShape) {
      onModify(image.id, currentShape);
    }
    setIsDrawing(false);
    setStartPoint(null);
    setCurrentShape(null);
  };

  return (
    <EditorContainer>
      {(
        <Stage
          width={size.width}
          height={size.height}
          onMouseDown={handleMouseDown}
          onMouseMove={handleMouseMove}
          onMouseUp={handleMouseUp}
        >
          <Layer>
            <KonvaImage image={imgRef.current} />
          {modifications.map((shape, i) => (
            shape.type === 'line' ? (
              <Line key={i} {...shape} />
            ) : (
              <Rect key={i} {...shape} />
            )
          ))}
          {currentShape && <Rect {...currentShape} />}
          </Layer>
        </Stage>
      )}
    </EditorContainer>
  );
};

// Стилизованные компоненты
const Container = styled.div`
  padding: 24px;
  max-width: 1600px;
  margin: 0 auto;
`;

const Toolbar = styled.div`
  margin-bottom: 24px;
  padding: 16px;
  background: #f8f9fa;
  border-radius: 8px;
`;

const ImageGrid = styled.div`
  margin-top: 20px;
`;

const EditorContainer = styled.div`
  position: relative;
  border: 2px solid #f0f0f0;
  border-radius: 8px;
  overflow: hidden;
  margin-bottom: 16px;
  background: white;

  canvas {
    width: 100% !important;
    height: auto !important;
    max-height: 600px;
  }
`;

const ImageInfo = styled.div`
  padding: 8px 16px;
  background: rgba(0, 0, 0, 0.65);
  color: white;
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  display: flex;
  justify-content: space-between;
  align-items: center;
`;

const ErrorContainer = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 60vh;
  text-align: center;
`;

export default GenerateDocPage;