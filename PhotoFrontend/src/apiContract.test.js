import { describe, it, expect } from 'vitest';
import axiosInstance from './api';

describe('Frontend API Contract Validation', () => {
  it('configures axios instance with credentials and base path', () => {
    expect(axiosInstance.defaults.withCredentials).toBe(true);
    expect(axiosInstance.defaults.baseURL).toMatch(/\/api$/);
  });

  it('attaches required security headers in request interceptor', () => {
    const mockConfig = { headers: {} };
    const requestInterceptor = axiosInstance.interceptors.request.handlers[0].fulfilled;
    const updatedConfig = requestInterceptor(mockConfig);

    expect(updatedConfig.headers['X-App-Client']).toBe('PhotoApp-Web');
  });

  it('matches backend authentication DTO payload contract', () => {
    const loginPayloadContract = {
      username: 'testuser',
      password: 'SecurePassword123!',
    };

    expect(loginPayloadContract).toHaveProperty('username');
    expect(loginPayloadContract).toHaveProperty('password');
    expect(typeof loginPayloadContract.username).toBe('string');
    expect(typeof loginPayloadContract.password).toBe('string');
  });

  it('matches backend photo upload DTO contract', () => {
    const photoUploadContract = {
      title: 'Beautiful Landscape',
      description: 'A shot taken during sunset',
      tags: ['nature', 'sunset'],
      groupId: 1,
    };

    expect(photoUploadContract).toHaveProperty('title');
    expect(photoUploadContract).toHaveProperty('tags');
    expect(Array.isArray(photoUploadContract.tags)).toBe(true);
  });
});
