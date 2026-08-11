import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import axiosInstance, { fetchCsrfToken } from './api';
import axios from 'axios';
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
        const getItemSpy = vi.spyOn(Storage.prototype, 'getItem');

        axiosInstance.defaults.adapter.mockResolvedValue({ data: 'ok', status: 200, headers: {} });

        await axiosInstance.get('/test');

        const config = axiosInstance.defaults.adapter.mock.calls[0][0];
        expect(config.headers['X-App-Client']).toBe('PhotoApp-Web');
        expect(axiosInstance.defaults.withCredentials).toBe(true);
        expect(config.headers.Authorization).toBeUndefined();

        // Ensure localStorage.getItem('token') is never called by the request interceptor to prevent XSS vulnerability regression
        expect(getItemSpy).not.toHaveBeenCalledWith('token');
        getItemSpy.mockRestore();
    });

    it('should not add Authorization header when token is absent', async () => {
        axiosInstance.defaults.adapter.mockResolvedValue({ data: 'ok', status: 200, headers: {} });

        await axiosInstance.get('/test');

        const config = axiosInstance.defaults.adapter.mock.calls[0][0];
        expect(config.headers['X-App-Client']).toBe('PhotoApp-Web');
        expect(axiosInstance.defaults.withCredentials).toBe(true);
        expect(config.headers.Authorization).toBeUndefined();
    });

    it('should deduplicate fetchCsrfToken calls when called concurrently', async () => {
        const spyGet = vi.spyOn(axios, 'get').mockResolvedValue({ data: { token: 'token-abc' } });

        const [t1, t2] = await Promise.all([fetchCsrfToken(), fetchCsrfToken()]);

        expect(t1).toBe('token-abc');
        expect(t2).toBe('token-abc');
        expect(spyGet).toHaveBeenCalledTimes(1);

        spyGet.mockRestore();
    });

    it('should refresh CSRF token and retry request once on CSRF error response', async () => {
        const spyGet = vi.spyOn(axios, 'get').mockResolvedValue({ data: { token: 'new-fresh-csrf-token' } });

        const initialError = {
            config: { method: 'post', url: '/photos/like', headers: { 'X-CSRF-TOKEN': 'stale-token' } },
            response: { status: 400, headers: { 'x-csrf-error': 'invalid_token' }, data: { code: 'INVALID_CSRF_TOKEN' } }
        };

        axiosInstance.defaults.adapter
            .mockRejectedValueOnce(initialError)
            .mockResolvedValueOnce({ data: { success: true }, status: 200, headers: {} });

        const result = await axiosInstance.post('/photos/like', { photoId: 1 });

        expect(spyGet).toHaveBeenCalledWith(expect.stringContaining('/Auth/csrf-token'), expect.anything());
        expect(result.data).toEqual({ success: true });

        spyGet.mockRestore();
    });

    it('should not retry infinitely if retried request also fails', async () => {
        const spyGet = vi.spyOn(axios, 'get').mockResolvedValue({ data: { token: 'new-fresh-csrf-token' } });

        const csrfError = {
            config: { method: 'post', url: '/photos/like', headers: { 'X-CSRF-TOKEN': 'stale-token' } },
            response: { status: 400, headers: { 'x-csrf-error': 'invalid_token' }, data: { code: 'INVALID_CSRF_TOKEN' } }
        };

        axiosInstance.defaults.adapter
            .mockRejectedValueOnce(csrfError)
            .mockRejectedValueOnce({ response: { status: 500 } });

        await expect(axiosInstance.post('/photos/like', { photoId: 1 })).rejects.toBeDefined();

        expect(toast.error).toHaveBeenCalledWith(
            "Erreur interne du serveur. Nos techniciens sont sur le coup !",
            { icon: '🔥' }
        );

        spyGet.mockRestore();
    });
});

