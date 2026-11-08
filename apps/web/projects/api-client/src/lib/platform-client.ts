import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ServiceStatus } from '../generated/types.gen';

@Injectable({ providedIn: 'root' })
export class PlatformClient {
  private readonly http = inject(HttpClient);

  getStatus() {
    return this.http.get<ServiceStatus>('/api/v1/status');
  }
}
