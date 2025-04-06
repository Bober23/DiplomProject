import React, { useState } from 'react';
import { useAuth } from '../components/AuthContext';
import { Button, Form, Input, Card, Spin, Row, Col, Alert } from 'antd';
import { LockOutlined, MailOutlined, UserAddOutlined } from '@ant-design/icons';
import { Link } from 'react-router-dom';

const LoginPage = () => {
  const { login, loading } = useAuth();
  const [form] = Form.useForm();
  const [error, setError] = useState(null);
  const onFinish = async (values) => {
    setError(null);
    try {
      await login(values.email, values.password);
    } catch (err) {
      // Устанавливаем ошибку только если это не 401 (он уже обработан в AuthContext)
        setError(err.message);
    }
  };

  return (
    <div style={{ 
      display: 'flex', 
      justifyContent: 'center', 
      alignItems: 'center', 
      height: '100vh',
      background: '#f0f2f5'
    }}>
      <Card
        title="Вход в систему"
        style={{ width: 400 }}
        headStyle={{ textAlign: 'center' }}
      >
        <Spin spinning={loading}>
          {/* Показываем ошибку, если она есть */}
          {error && (
            <Alert
              message={error}
              type="error"
              showIcon
              style={{ marginBottom: 24 }}
            />
          )}
          <Form
            form={form}
            name="login"
            onFinish={onFinish}
            autoComplete="off"
          >
            <Form.Item
              name="email"
              rules={[
                { required: true, message: 'Пожалуйста, введите email' },
                { type: 'email', message: 'Некорректный email' }
              ]}
            >
              <Input 
                prefix={<MailOutlined />} 
                placeholder="Email" 
              />
            </Form.Item>

            <Form.Item
              name="password"
              rules={[{ required: true, message: 'Пожалуйста, введите пароль' }]}
            >
              <Input.Password
                prefix={<LockOutlined />}
                placeholder="Пароль"
              />
            </Form.Item>

            <Form.Item>
              <Button 
                type="primary" 
                htmlType="submit" 
                block
                loading={loading}
              >
                Войти
              </Button>
            </Form.Item>
            <Row justify="center">
              <Col>
                <Link to="/register">
                  <Button 
                    icon={<UserAddOutlined />} 
                    type="link"
                  >
                    Создать пользователя
                  </Button>
                </Link>
              </Col>
            </Row>
          </Form>
        </Spin>
      </Card>
    </div>
  );
};

export default LoginPage;