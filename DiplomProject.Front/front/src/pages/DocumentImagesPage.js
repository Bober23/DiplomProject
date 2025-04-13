import React, { useState, useEffect, useRef } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Button,
  Row,
  Col,
  Card,
  Image,
  Space,
  Spin,
  Typography,
  Progress,
  message
} from 'antd';
import { PlusOutlined, FileAddOutlined, CloseCircleOutlined, DeleteFilled } from '@ant-design/icons';
import styled from 'styled-components';
import { useAuth } from '../components/AuthContext';
import JSZip from 'jszip';

const { Title, Text } = Typography;

const DocumentImages = () => {
  const { documentId } = useParams();
  const { user } = useAuth();
  const [document, setDocument] = useState(null);
  const [images, setImages] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [progress, setProgress] = useState(0);
  const [processing, setProcessing] = useState(false);
  const fileInputRef = useRef(null);
  const [uploading, setUploading] = useState(false);
  const navigate = useNavigate();
  const apiUrl = process.env.REACT_APP_API_URL;

  // Очистка Blob URL при размонтировании
  useEffect(() => {
    return () => {
      images.forEach(image => URL.revokeObjectURL(image.url));
    };
  }, [images]);

  // Загрузка данных документа и изображений
  useEffect(() => {
    const abortController = new AbortController();
    fetchDocumentData(abortController);
    return () => abortController.abort();
  }, [documentId, user.token]);

  const fetchDocumentData = async (abortController) => {
    try {
      setLoading(true);
      setError(null);

      // Загрузка метаданных документа
      const docResponse = await fetch(
        `${apiUrl}/api/Document/${documentId}`,
        {
          headers: {
            'Authorization': `Bearer ${user.token}`
          },
          signal: abortController.signal
        }
      );

      if (!docResponse.ok) throw new Error('Ошибка загрузки документа');
      const docData = await docResponse.json();
      setDocument(docData);

      // Загрузка ZIP-архива
      const zipResponse = await fetch(
        `${apiUrl}/api/Document/images/${documentId}`,
        {
          headers: {
            'Authorization': `Bearer ${user.token}`
          },
          signal: abortController.signal
        }
      );

      if (!zipResponse.ok) throw new Error('Ошибка загрузки изображений');

      // Обработка ZIP-архива
      setProcessing(true);
      const zipData = await zipResponse.blob();
      const zip = await JSZip.loadAsync(zipData);

      const files = Object.values(zip.files).filter(file => !file.dir);

      const MAX_PARALLEL = 4;
      const chunks = [];
      for (let i = 0; i < files.length; i += MAX_PARALLEL) {
        chunks.push(files.slice(i, i + MAX_PARALLEL));
      }

      const imagesArray = [];
      let processed = 0;

      for (const chunk of chunks) {
        await Promise.all(chunk.map(async (zipEntry) => {
          try {
            const fileData = await zipEntry.async('blob');
            const url = URL.createObjectURL(fileData);

            imagesArray.push({
              id: zipEntry.name,
              name: zipEntry.name.split('/').pop(),
              url: url,
              size: zipEntry._data.uncompressedSize
            });

            processed++;
            setProgress(Math.round((processed / files.length) * 100));
          } catch (e) {
            console.error(`Ошибка обработки файла ${zipEntry.name}:`, e);
          }
        }));
      }

      setImages(imagesArray.sort((a, b) => a.name.localeCompare(b.name)));
      setProcessing(false);

    } catch (err) {
      if (abortController.signal.aborted) return;
      setError(err.message);
      setProcessing(false);
    } finally {
      if (!abortController.signal.aborted) {
        setLoading(false);
      }
    }
  };

  const handleGenerateDocument = () => {
    navigate(`/documents/${documentId}/generate`, {});
  };

  const handleDeleteImage = async (image) => {
    try {
      console.log(document);
      console.log(image);
      const response = await fetch(
        `${apiUrl}/api/Document/${documentId}/${document.imageFiles.find(img => img.name === image.name)?.id}`,
        {
          method: 'DELETE',
          headers: {
            'Authorization': `Bearer ${user.token}`
          },
        }
      );

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Ошибка удаления изображения');
      }


      message.success('Изображение успешно удалено');
    } catch (error) {
      message.error(error.message);
    } finally{
      const abortController = new AbortController();
      fetchDocumentData(abortController);
    }
  }

  const handleFileSelect = async (e) => {
    const file = e.target.files[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      message.error('Пожалуйста, выберите файл изображения');
      return;
    }

    try {
      setUploading(true);
      const formData = new FormData();
      formData.append('file', file);

      const response = await fetch(
        `${apiUrl}/api/Document/image/${documentId}`,
        {
          method: 'PATCH',
          headers: {
            'Authorization': `Bearer ${user.token}`
          },
          body: formData
        }
      );

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Ошибка загрузки изображения');
      }

      // Обновляем список изображений
      const newImage = await response.json();
      setImages(prev => [...prev, {
        id: newImage.id,
        name: newImage.fileName,
        url: URL.createObjectURL(file),
        size: file.size
      }]);

      message.success('Изображение успешно добавлено');
    } catch (error) {
      message.error(error.message);
    } finally {
      setUploading(false);
      e.target.value = ''; // Сброс input
    }
  };

  // Обновленная кнопка добавления
  const AddImageButton = () => (
    <>
      <Button
        type="primary"
        icon={<PlusOutlined />}
        onClick={() => fileInputRef.current.click()}
        loading={uploading}
        disabled={processing}
      >
        {uploading ? 'Загрузка...' : 'Добавить изображение'}
      </Button>
      <input
        type="file"
        ref={fileInputRef}
        style={{ display: 'none' }}
        accept="image/*"
        onChange={handleFileSelect}
      />
    </>
  );

  if (error) {
    return (
      <Container>
        <ErrorContainer>
          <CloseCircleOutlined style={{ fontSize: 48, color: '#ff4d4f' }} />
          <Title level={4} style={{ marginTop: 16 }}>Произошла ошибка</Title>
          <Text type="danger">{error}</Text>
          <Button
            type="primary"
            style={{ marginTop: 24 }}
            onClick={() => navigate(-1)}
          >
            Вернуться назад
          </Button>
        </ErrorContainer>
      </Container>
    );
  }

  if (!document || loading) {
    return <Spin size="large" fullscreen />;
  }

  return (
    <Container>
      {/* Шапка документа */}
      <DocumentHeader>
        <Title level={3}>{document.name}</Title>
        <Space>
          <AddImageButton />
          <Button
            type="default"
            icon={<FileAddOutlined />}
            onClick={handleGenerateDocument}
            disabled={processing}
          >
            Сгенерировать {document.extension}
          </Button>
        </Space>
      </DocumentHeader>

      {/* Информация о документе */}
      <DocumentInfo>
        <InfoItem>
          <Label>Формат:</Label>
          <Value>{document.extension}</Value>
        </InfoItem>
        <InfoItem>
          <Label>Категория:</Label>
          <Value>{document.category || 'Не указана'}</Value>
        </InfoItem>
      </DocumentInfo>

      {/* Прогресс-бар */}
      {processing && (
        <ProgressContainer>
          <Progress
            percent={progress}
            status="active"
            strokeColor={{
              '0%': '#108ee9',
              '100%': '#87d068',
            }}
          />
          <Text type="secondary">
            Обработка изображений: {progress}%
          </Text>
        </ProgressContainer>
      )}

      {/* Галерея изображений */}
      <ImageGrid>
        <Row gutter={[16, 16]}>
          {images.map((image) => (
            <Col key={image.id} xs={24} sm={12} md={8} lg={6} xl={4}>
              <ImageCard
                hoverable
                cover={
                  <StyledImage
                    src={image.url}
                    alt={image.name}
                    preview={true}
                  />
                }
              >
                <Card.Meta
                  title={image.name}
                  description={
                    <>
                      <div>Размер: {(image.size / 1024).toFixed(2)} KB</div>
                    </>
                  }
                />
                <Button
                  type="default"
                  icon={<DeleteFilled />}
                  onClick={() => handleDeleteImage(image)}
                >
                  Удалить
                </Button>
              </ImageCard>
            </Col>
          ))}
        </Row>
      </ImageGrid>
    </Container>
  );
};

// Стилизованные компоненты
const Container = styled.div`
  padding: 24px;
  max-width: 1600px;
  margin: 0 auto;
`;

const DocumentHeader = styled.div`
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
  margin-left:16px;
  @media (max-width: 768px) {
    flex-direction: column;
    gap: 16px;
    align-items: flex-start;
  }
`;

const DocumentInfo = styled.div`
  display: flex;
  gap: 24px;
  margin-bottom: 32px;
  padding: 16px;
  background: #f8f9fa;
  border-radius: 8px;
  flex-wrap: wrap;

  @media (max-width: 480px) {
    gap: 12px;
  }
`;

const InfoItem = styled.div`
  display: flex;
  flex-direction: column;
  min-width: 200px;
`;

const Label = styled.span`
  font-weight: 500;
  color: #666;
  font-size: 14px;
`;

const Value = styled.span`

  font-size: 16px;
  color: #333;
`;

const ImageGrid = styled.div`
  margin-top: 20px;
`;

const ImageCard = styled(Card)`
  border-radius: 8px;
  overflow: hidden;
  transition: transform 0.2s;

  &:hover {
    transform: translateY(-4px);
    box-shadow: 0 8px 16px rgba(0, 0, 0, 0.1);
  }

  .ant-card-meta-description {
    font-size: 12px;
    color: #666;
  }
`;

const StyledImage = styled(Image)`
  width: 100%;
  height: 200px;
  object-fit: cover;
  background: #f0f2f5;
  border-radius: 8px 8px 0 0;
`;

const ErrorContainer = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 60vh;
  text-align: center;
  gap: 8px;
`;

const ProgressContainer = styled.div`
  margin: 24px 0;
  text-align: center;
`;

export default DocumentImages;