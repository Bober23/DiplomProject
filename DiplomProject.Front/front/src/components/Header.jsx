import React, { useState, useRef, useEffect } from 'react';
import { useAuth } from './AuthContext';
import { Button, Modal, Form, Input, message } from 'antd';
import { UserOutlined, LogoutOutlined, LockOutlined, DeleteOutlined, QuestionCircleOutlined } from '@ant-design/icons';
import styled from 'styled-components';
import { useNavigate } from 'react-router-dom';
import CryptoJS from 'crypto-js';

const HeaderContainer = styled.header`
  height: 70px;
  background-color: #2383E2;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 40px;
  color: white;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  z-index: 1000;
`;

const LogoSection = styled.div`
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 20px;
  font-weight: 600;
  cursor: pointer;
`;

const ProfileMenu = styled.div`
  position: relative;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  border-radius: 4px;
  transition: background-color 0.3s;

  &:hover {
    background-color: rgba(255, 255, 255, 0.1);
  }
`;

const DropdownMenu = styled.div`
  position: absolute;
  top: 50px;
  right: 0;
  background: white;
  border-radius: 4px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  min-width: 200px;
  z-index: 1001;
`;

const MenuItem = styled.div`
  padding: 12px 16px;
  color: #333;
  display: flex;
  align-items: center;
  gap: 8px;
  transition: background-color 0.3s;

  &:hover {
    background-color: #f5f5f5;
  }
`;

const Header = () => {
    const { user, logout } = useAuth();
    const [showMenu, setShowMenu] = useState(false);
    const [changePasswordVisible, setChangePasswordVisible] = useState(false);
    const [deleteAccountVisible, setDeleteAccountVisible] = useState(false);
    const [loading, setLoading] = useState(false);
    const [form] = Form.useForm();
    const menuRef = useRef(null);
    const [supportVisible, setSupportVisible] = useState(false);
    const [supportLoading, setSupportLoading] = useState(false);
    const [supportText, setSupportText] = useState('');
    const apiUrl = process.env.REACT_APP_API_URL;
    const navigate = useNavigate();


    const handleChangePassword = async (values) => {
        try {
            const response = await fetch(`${apiUrl}/api/User/${user.id}?passwordHash=${CryptoJS.SHA256(values.passwordHash).toString()}`, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${user.token}` // Если используется JWT
                },
            });

            if (!response.ok) {
                throw new Error('Ошибка при изменении пароля');
            }

            message.success('Пароль успешно изменён');
            setChangePasswordVisible(false);
            form.resetFields();
        } catch (error) {
            message.error(error.message);
        }
    };

    const handleDeleteAccount = async () => {
        setLoading(true);
        try {
            const response = await fetch(`${apiUrl}/api/User/${user.id}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${user.token}` // Если используется JWT
                }
            });

            if (!response.ok) {
                throw new Error('Ошибка при удалении аккаунта');
            }

            message.success('Аккаунт успешно удалён');
            logout(); // Выходим из системы после удаления
            navigate('/login');
        } catch (error) {
            message.error(error.message);
        } finally {
            setLoading(false);
            setDeleteAccountVisible(false);
        }
    };

    const handleClickOutside = (event) => {
        if (menuRef.current && !menuRef.current.contains(event.target)) {
            setShowMenu(false);
        }
    };

    useEffect(() => {
        document.addEventListener('mousedown', handleClickOutside);
        return () => document.removeEventListener('mousedown', handleClickOutside);
    }, []);

    const handleHomeClick = () => {
        navigate(`/`);
    };

    const handleMenuItemClick = (action) => {
        setShowMenu(false);
        switch (action) {
            case 'logout':
                logout();
                navigate('/login');
                break;
            // Добавьте обработку других действий
            case 'support':
                setSupportVisible(true);
                break;
        }
    };
    if (!user) return null;

    return (
        <>
            <HeaderContainer>
                <LogoSection>
                    <span onClick={handleHomeClick}>TextScanner</span>
                </LogoSection>

                <ProfileMenu onClick={() => setShowMenu(!showMenu)} ref={menuRef}>
                    <UserOutlined style={{ fontSize: '24px' }} />
                    <span>{user.email.split('@')[0]}</span>

                    {showMenu && (
                        <DropdownMenu>
                            <MenuItem onClick={() => {
                                setChangePasswordVisible(true);
                                setShowMenu(false);
                            }}>
                                <LockOutlined />
                                Сменить пароль
                            </MenuItem>
                            <MenuItem onClick={() => {
                                setDeleteAccountVisible(true);
                                setShowMenu(false);
                            }}>
                                <DeleteOutlined />
                                Удалить аккаунт
                            </MenuItem>
                            <MenuItem onClick={() => handleMenuItemClick('support')}>
                                <QuestionCircleOutlined />
                                Техподдержка
                            </MenuItem>
                            <MenuItem
                                onClick={() => handleMenuItemClick('logout')}
                                style={{ borderTop: '1px solid #eee', color: '#ff4d4f' }}
                            >
                                <LogoutOutlined />
                                Выйти
                            </MenuItem>
                        </DropdownMenu>
                    )}
                </ProfileMenu>
            </HeaderContainer>
            {/* Модальное окно смены пароля */}
            <Modal
                title="Смена пароля"
                open={changePasswordVisible}
                onCancel={() => {
                    setChangePasswordVisible(false);
                    form.resetFields();
                }}
                footer={null}
            >
                <Form
                    form={form}
                    onFinish={handleChangePassword}
                >
                    <Form.Item
                        name="passwordHash"
                        rules={[
                            { required: true, message: 'Введите новый пароль' },
                            { min: 6, message: 'Пароль должен быть не менее 6 символов' }
                        ]}
                    >
                        <Input.Password placeholder="Новый пароль" />
                    </Form.Item>

                    <Form.Item
                        name="confirmPassword"
                        dependencies={['passwordHash']}
                        rules={[
                            { required: true, message: 'Подтвердите пароль' },
                            ({ getFieldValue }) => ({
                                validator(_, value) {
                                    if (!value || getFieldValue('passwordHash') === value) {
                                        return Promise.resolve();
                                    }
                                    return Promise.reject(new Error('Пароли не совпадают'));
                                },
                            }),
                        ]}
                    >
                        <Input.Password placeholder="Подтвердите пароль" />
                    </Form.Item>

                    <Form.Item>
                        <Button
                            type="primary"
                            htmlType="submit"
                            block
                        >
                            Сохранить новый пароль
                        </Button>
                    </Form.Item>
                </Form>
            </Modal>
            <Modal
                title="Подтверждение удаления"
                open={deleteAccountVisible}
                onCancel={() => setDeleteAccountVisible(false)}
                footer={[
                    <Button
                        key="cancel"
                        onClick={() => setDeleteAccountVisible(false)}
                    >
                        Отмена
                    </Button>,
                    <Button
                        key="delete"
                        type="primary"
                        danger
                        loading={loading}
                        onClick={handleDeleteAccount}
                    >
                        Да, удалить
                    </Button>,
                ]}
            >
                <p>Вы точно хотите удалить аккаунт? Это действие нельзя отменить.</p>
                <p>Все ваши данные будут безвозвратно удалены.</p>
            </Modal>
            <Modal
                title="Обращение в техподдержку"
                open={supportVisible}
                onCancel={() => setSupportVisible(false)}
                footer={[
                    <Button
                        key="back"
                        onClick={() => setSupportVisible(false)}
                    >
                        Отмена
                    </Button>,
                    <Button
                        key="submit"
                        type="primary"
                        loading={supportLoading}
                        onClick={async () => {
                            if (!supportText.trim()) {
                                message.error('Введите текст сообщения');
                                return;
                            }

                            setSupportLoading(true);
                            try {
                                // Здесь будет реальный запрос к API
                                await fetch(`${apiUrl}/api/BugReport`, {
                                    method: 'POST',
                                    headers: {
                                        'Content-Type': 'application/json',
                                        'Authorization': `Bearer ${user.token}`
                                    },
                                    body: JSON.stringify({
                                        message: supportText,
                                    })
                                });

                                message.success('Сообщение отправлено!');
                                setSupportVisible(false);
                                setSupportText('');
                            } catch (error) {
                                message.error('Ошибка при отправке сообщения');
                            } finally {
                                setSupportLoading(false);
                            }
                        }}
                    >
                        Отправить
                    </Button>,
                ]}
            >
                <Input.TextArea
                    rows={4}
                    value={supportText}
                    onChange={(e) => setSupportText(e.target.value)}
                    placeholder="Опишите проблему"
                />
            </Modal>
        </>
    );
};

export default Header;