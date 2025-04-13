import React, { useState, useEffect } from 'react';
import { useAuth } from '../components/AuthContext';
import { Link, useNavigate } from 'react-router-dom';
import { Table, Button, Space, Spin, message, Tag, Modal, Form, Input, Select } from 'antd';
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
  const navigate = useNavigate();
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [isCreateModalVisible, setIsCreateModalVisible] = useState(false);
  const [form] = Form.useForm();
  const apiUrl = process.env.REACT_APP_API_URL;

  useEffect(() => {
    const fetchDocuments = async () => {
      try {
        const response = await fetch(`${apiUrl}/api/Document/user/${user.id}`, {
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
    setEditingDocument(documents.find(document => document.id === documentId));
    console.log(documentId);
    form.setFieldsValue({
      name: document.name,
      category: document.category
    });
    setIsModalVisible(true);
  };

  const handleDownload = async (documentId) => {
    try {
      setLoading(true);
      const response = await fetch(`${apiUrl}/api/Document/docfile/${documentId}`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });

      if (!response.ok) {
        throw new Error('Ошибка при получении файла');
      }

      // Получаем имя файла из заголовков или генерируем
      const contentDisposition = response.headers.get('content-disposition');

      let filename = documents.find(document => document.id === documentId).name;
      // Получаем blob из ответа
      const blob = await response.blob();

      // Создаем ссылку для скачивания
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      if (documents.find(document => document.id === documentId).extension === "DOCX") {
        a.download = filename + ".docx";
      }
      else {
        a.download = filename;
      }

      document.body.appendChild(a);
      a.click();

      // Очищаем
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);

      message.success('Скачивание начато');
    } catch (error) {
      message.error(error.message || 'Не удалось скачать файл');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateDocument = async () => {
    try {
      const values = await form.validateFields();
      setLoading(true);

      const response = await fetch(`${apiUrl}/api/Document`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({
          name: values.name,
          category: values.category,
          extension: values.format,
          authorId: user.id
        }),
      });

      if (!response.ok) {
        throw new Error('Ошибка создания документа');
      }

      const newDocument = await response.json();
      navigate(`/documents/${newDocument.id}/images`);
      setDocuments([...documents, newDocument]);
      message.success('Документ успешно создан');
      setIsCreateModalVisible(false);
      form.resetFields();
    } catch (error) {
      message.error(error.message);
    } finally {
      setLoading(false);
    }
  };
  const showCreateModal = () => {
    setIsCreateModalVisible(true);
  };

  const handleOpenImages = () => {
    navigate(`/documents/${editingDocument.id}/images`);
  };

  const handleAddImageDocument = async (id) => {
    navigate(`/documents/${id}/images`);
  };

  const handleSaveChanges = async () => {
    try {
      const values = await form.validateFields();
      setLoading(true);

      const response = await fetch(`${apiUrl}/api/Document/namecat/${editingDocument.id}`, {
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
      const response = await fetch(`${apiUrl}/api/Document/${documentId}`, {
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
              onClick={() => handleDownload(record.id)}
            />
          )}

          <Button
            icon={<FileAddOutlined />}
            type="primary"
            onClick={() => handleAddImageDocument(record.id)}
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
        onClick={showCreateModal}
      >
        Создать новый документ
      </Button>
      <Modal
        title="Создание нового документа"
        visible={isCreateModalVisible}
        onCancel={() => setIsCreateModalVisible(false)}
        footer={[
          <Button key="cancel" onClick={() => setIsCreateModalVisible(false)}>
            Отмена
          </Button>,
          <Button
            key="create"
            type="primary"
            icon={<SaveOutlined />}
            loading={loading}
            onClick={handleCreateDocument}
          >
            Создать
          </Button>,
        ]}
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="name"
            label="Название документа"
            rules={[{ required: true, message: 'Введите название документа' }]}
          >
            <Input placeholder="Введите название" />
          </Form.Item>

          <Form.Item
            name="format"
            label="Формат"
            rules={[{ required: true, message: 'Выберите формат документа' }]}
          >
            <Select placeholder="Выберите формат">
              <Select.Option value="DOCX">DOCX</Select.Option>
              <Select.Option value="PDF">PDF</Select.Option>
              <Select.Option value="ODT">ODT</Select.Option>
            </Select>
          </Form.Item>

          <Form.Item
            name="category"
            label="Категория"
          >
            <Input
              placeholder="Выберите или введите категорию"
            />
          </Form.Item>
        </Form>
      </Modal>
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
            onClick={handleSaveChanges}
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
            <Input value={editingDocument?.name} />
          </Form.Item>

          <Form.Item
            name="category"
            label="Категория"
            rules={[{ required: false, message: 'Введите категорию' }]}
          >
            <Input value={editingDocument?.category} />
          </Form.Item>
        </Form>
        <Button
          type="primary"
          loading={loading}
          onClick={handleOpenImages}
        >
          Редактировать изображения документа
        </Button>
      </Modal>
    </div>
  );
};

export default DocumentListPage;