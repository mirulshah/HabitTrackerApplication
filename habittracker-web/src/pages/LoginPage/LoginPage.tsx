import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { login, type LoginRequest } from '../../api/auth';
import { useAuth } from '../../auth/useAuth';
import styles from './LoginPage.module.css';

export function LoginPage() {
    const navigate = useNavigate();
    const { loginSuccess } = useAuth();
    const {
        register,
        handleSubmit,
        formState: { errors },
    } = useForm<LoginRequest>();

    const mutation = useMutation({
        mutationFn: login,
        onSuccess: (data) => {
            loginSuccess(data.accessToken, data.refreshToken);
            navigate('/tasks');
        },
    });

    function onSubmit(data: LoginRequest) {
        mutation.mutate(data);
    }

    return (
        <div className="min-h-screen flex flex-col items-center justify-center bg-[url('/login-bg.jpg')] bg-cover bg-center bg-no-repeat">
            <div className={`${styles.header}`}>
                <h1 className="text-7xl font-bold text-gray-800 mb-8">Habit Tracker</h1>
            </div>
            <form onSubmit={handleSubmit(onSubmit)} className="bg-sky-200 p-8 rounded shadow-md w-full max-w-md mt-12">
                <h1 className="text-2xl font-semibold mb-6 text-gray-700">Login</h1>
                <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
                    <input type="email" {...register('email', { required: 'Email is required' })} className="border-2 bg-white border-gray-300 rounded py-2 px-3 focus:outline-none focus:ring-2 focus:ring-blue-500 w-full"/>
                    {errors.email && (<p className="text-red-600 text-sm mt-1">{errors.email.message}</p>)}
                </div>

                <div className="mb-6">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Password</label>
                    <input type="password" {...register('password', { required: 'Password is required' })} className="border-2 bg-white border-gray-300 rounded py-2 px-3 focus:outline-none focus:ring-2 focus:ring-blue-500 w-full" />
                    {errors.password && (<p className="text-red-600 text-sm mt-1">{errors.password.message}</p>)}
                </div>

                {mutation.isError && (
                    <p className="text-red-600 text-sm mt-1">Invalid email or password</p>
                )}

                <button type="submit" disabled={mutation.isPending} className="w-full bg-blue-600 text-white py-2 rounded-md hover:bg-blue-700 disabled:opacity-50">
                    {mutation.isPending ? 'Logging in...' : 'Login'}
                </button>
            </form>
        </div>
    );
}