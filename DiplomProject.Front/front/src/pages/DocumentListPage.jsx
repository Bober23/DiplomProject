import React, { useState, useEffect } from 'react';
import { useAuth } from '../components/AuthContext';
import { Table, Button, Space, Spin, message, Tag, Modal, Form, Input } from 'antd';
import { 
  PlusOutlined, 
  EditOutlined, 
  DownloadOutlined, 
  DeleteOutlined,
  FileAddOutlined,
  SaveOutlined
} from '@ant-design/icons';

const DocumentListPage = () => {
  const { user } = useAuth();
  const [documents, setDocuments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [editingDocument, setEditingDocument] = useState(null);
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [form] = Form.useForm();

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

  const handleEdit = (documentId) => {
    setEditingDocument(documents.find(document=>document.id === documentId));
    console.log(documentId);
    form.setFieldsValue({
      name: document.name,
      category: document.category
    });
    setIsModalVisible(true);
  };

  const handleDownload = (documentId, contentLink) => {
    if (contentLink) {
      // Реальная логика скачивания
      window.open(contentLink, '_blank');
      message.success('Начато скачивание документа');
    } else {
      message.warning('Документ недоступен для скачивания');
    }
  };

  const handleCreateDocument = (documentId) => {
    message.info(`Создание документа ${documentId} (заглушка)`);
    // Здесь будет запрос на сервер для создания документа
  };

  const handleSave = async () => {
    try {
      const values = await form.validateFields();
      setLoading(true);
      
      const response = await fetch(`http://localhost:5120/api/Document/namecat/${editingDocument.id}`, {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({
          name: values.name,
          category: values.category
        }),
      });

      if (!response.ok) {
        throw new Error('Ошибка сохранения изменений');
      }

      // Обновляем локальное состояние
      setDocuments(documents.map(doc => 
        doc.id === editingDocument.id 
          ? { ...doc, name: values.name, category: values.category } 
          : doc
      ));

      message.success('Изменения сохранены');
      setIsModalVisible(false);
    } catch (error) {
      message.error(error.message);
    } finally {
      setLoading(false);
    }
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
      title: 'Статус',
      key: 'status',
      render: (_, record) => (
        record.contentLink ?
          <Tag color="green">Готов</Tag> :
          <Tag color="red">Не создан</Tag>
      )
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
          {record.contentLink && (
            <Button
              icon={<DownloadOutlined />}
              onClick={() => handleDownload(record.id, record.contentLink)}
            />
          )}
          {record.imageFiles.length > 0 && (
            <Button
              icon={<FileAddOutlined />}
              type="primary"
              onClick={() => handleCreateDocument(record.id)}
            />
          )}

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
      {/* Модальное окно редактирования */}
      <Modal
        title={`Редактирование документа ${editingDocument?.name || ''}`}
        visible={isModalVisible}
        onCancel={() => setIsModalVisible(false)}
        footer={[
          <Button key="cancel" onClick={() => setIsModalVisible(false)}>
            Отмена
          </Button>,
          <Button 
            key="save" 
            type="primary" 
            icon={<SaveOutlined />}
            loading={loading}
            onClick={handleSave}
          >
            Сохранить
          </Button>,
        ]}
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="name"
            label="Название документа"
            rules={[{ required: true, message: 'Введите название' }]}
          >
            <Input value={editingDocument?.name}/>
          </Form.Item>

          <Form.Item
            name="category"
            label="Категория"
            rules={[{ required: true, message: 'Введите категорию' }]}
          >
            <Input value={editingDocument?.category}/>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default DocumentListPage;