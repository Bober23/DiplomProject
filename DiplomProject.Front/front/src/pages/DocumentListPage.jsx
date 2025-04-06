import React, { useState, useEffect } from 'react';
import { useAuth } from '../components/AuthContext';
import { Table, Button, Space, Spin, message } from 'antd';
import { 
  PlusOutlined, 
  EditOutlined, 
  DownloadOutlined, 
  DeleteOutlined 
} from '@ant-design/icons';

const DocumentListPage = () => {
  const { user } = useAuth();
  const [documents, setDocuments] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchDocuments = async () => {
      try {
        const response = await fetch(`http://localhost:5120/api/Document/user/${user.id}`, {
          headers: {
            'Authorization': `Bearer ${user.token}` // если используется JWT
          }
        });

        if (!response.ok) {
          throw new Error('Ошибка загрузки документов');
        }

        const data = await response.json();
        setDocuments(data);
      } catch (error) {
        message.error(error.message);
      } finally {
        setLoading(false);
      }
    };

    fetchDocuments();
  }, [user.id, user.token]);

  const handleDownload = (documentId) => {
    message.info(`Скачивание документа ${documentId} (заглушка)`);
  };

  const handleEdit = (documentId) => {
    message.info(`Редактирование документа ${documentId} (заглушка)`);
  };

  const handleDelete = async (documentId) => {
    try {
      const response = await fetch(`http://localhost:5120/api/Document/${documentId}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });
      
      if (!response.ok) throw new Error('Ошибка удаления');
      
      message.success('Документ удалён');
      setDocuments(documents.filter(doc => doc.id !== documentId));
    } catch (error) {
      message.error(error.message);
    }
  };

  const columns = [
    {
      title: 'Название',
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: 'Формат',
      dataIndex: 'extension',
      key: 'extension',
      render: text => <span style={{ textTransform: 'uppercase' }}>{text}</span>
    },
    {
      title: 'Категория',
      dataIndex: 'category',
      key: 'category',
    },
    {
      title: 'Действия',
      key: 'actions',
      render: (_, record) => (
        <Space size="middle">
          <Button
            icon={<EditOutlined />}
            onClick={() => handleEdit(record.id)}
          />
          <Button
            icon={<DownloadOutlined />}
            onClick={() => handleDownload(record.id)}
          />
          <Button
            icon={<DeleteOutlined />}
            danger
            onClick={() => handleDelete(record.id)}
          />
        </Space>
      ),
    },
  ];

  return (
    <div style={{ padding: '24px' }}>
      <Button 
        type="primary" 
        icon={<PlusOutlined />} 
        style={{ marginBottom: 16 }}
      >
        Создать новый документ
      </Button>

      <Spin spinning={loading}>
        <Table 
          dataSource={documents}
          columns={columns}
          rowKey="id"
          pagination={{ pageSize: 10 }}
        />
      </Spin>
    </div>
  );
};

export default DocumentListPage;