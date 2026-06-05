import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import axiosInstance from './api';
import toast from 'react-hot-toast';

vi.mock('react-hot-toast');

describe('api.js interceptors', () => {
    let originalLocation;

    beforeEach(() => {
        vi.clearAllMocks();
        localStorage.clear();

        originalLocation = window.location;
        delete window.location;
        window.location = { pathname: '/', href: '' };

        axiosInstance.defaults.adapter = vi.fn();
    });

    afterEach(() => {
        window.location = originalLocation;
    });

    it('should show toast when there is no response (network error)', async () => {
        const networkError = new Error('Network Error');
        // Simulate error without response
        axiosInstance.defaults.adapter.mockRejectedValue(networkError);

        await expect(axiosInstance.get('/test')).rejects.toThrow('Network Error');

        expect(toast.error).toHaveBeenCalledWith(
            "Serveur injoignable. Le service est temporairement indisponible.",
            expect.objectContaining({ icon: '🔌' })
        );
    });

    it('should remove token and redirect on 401 error', async () => {
        const error = { response: { status: 401 } };
        axiosInstance.defaults.adapter.mockRejectedValue(error);

        localStorage.setItem('user_info', JSON.stringify({role: 'User'}));

        await expect(axiosInstance.get('/test')).rejects.toEqual(error);

        expect(localStorage.getItem('user_info')).toBeNull();
        expect(window.location.href).toBe('/login?ejected=true');
    });

    it('should remove token and redirect on 403 error', async () => {
        const error = { response: { status: 403 } };
        axiosInstance.defaults.adapter.mockRejectedValue(error);

        localStorage.setItem('user_info', JSON.stringify({role: 'User'}));

        await expect(axiosInstance.get('/test')).rejects.toEqual(error);

        expect(localStorage.getItem('user_info')).toBeNull();
        expect(window.location.href).toBe('/login?ejected=true');
    });

    it('should not redirect if already on /login', async () => {
        window.location.pathname = '/login';
        const error = { response: { status: 401 } };
        axiosInstance.defaults.adapter.mockRejectedValue(error);

        localStorage.setItem('user_info', JSON.stringify({role: 'User'}));

        await expect(axiosInstance.get('/test')).rejects.toEqual(error);

        expect(localStorage.getItem('user_info')).toBe(JSON.stringify({role: 'User'}));
        expect(window.location.href).toBe('');
    });

    it('should show toast on 500 error', async () => {
        const error = { response: { status: 500 } };
        axiosInstance.defaults.adapter.mockRejectedValue(error);

        await expect(axiosInstance.get('/test')).rejects.toEqual(error);

        expect(toast.error).toHaveBeenCalledWith(
            "Erreur interne du serveur. Nos techniciens sont sur le coup !",
            { icon: '🔥' }
        );
    });

    it('should return error for other status codes', async () => {
        const error = { response: { status: 404 } };
        axiosInstance.defaults.adapter.mockRejectedValue(error);

        await expect(axiosInstance.get('/test')).rejects.toEqual(error);

        // Assert no toast or redirection happened
        expect(toast.error).not.toHaveBeenCalled();
        expect(window.location.href).toBe('');
    });

    it('should add X-App-Client header, but no Authorization header (using withCredentials instead)', async () => {

        axiosInstance.defaults.adapter.mockResolvedValue({ data: 'ok', status: 200, headers: {} });

        await axiosInstance.get('/test');

        const config = axiosInstance.defaults.adapter.mock.calls[0][0];
        expect(config.headers['X-App-Client']).toBe('PhotoApp-Web');
        expect(axiosInstance.defaults.withCredentials).toBe(true);
        expect(config.headers.Authorization).toBeUndefined();
    });

    it('should not add Authorization header when token is absent', async () => {
        axiosInstance.defaults.adapter.mockResolvedValue({ data: 'ok', status: 200, headers: {} });

        await axiosInstance.get('/test');

        const config = axiosInstance.defaults.adapter.mock.calls[0][0];
        expect(config.headers['X-App-Client']).toBe('PhotoApp-Web');
        expect(axiosInstance.defaults.withCredentials).toBe(true);
        expect(config.headers.Authorization).toBeUndefined();
    });
});
