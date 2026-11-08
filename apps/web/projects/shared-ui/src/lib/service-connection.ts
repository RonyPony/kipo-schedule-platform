import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { PlatformClient } from 'api-client';
import { MatButtonModule } from '@angular/material/button';
import { finalize, timeout } from 'rxjs';

@Component({
  selector: 'kipo-service-connection',
  imports: [MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section aria-labelledby="connection-title" aria-live="polite" [attr.aria-busy]="loading()">
      <h2 id="connection-title">Conexión con Kipo</h2>
      <p>{{ message() }}</p>
      @if (error()) {
        <p role="alert">{{ error() }}</p>
      }
      <button mat-stroked-button (click)="check()" [disabled]="loading()">
        {{ loading() ? 'Comprobando…' : 'Comprobar conexión' }}
      </button>
    </section>
  `,
  styles: `
    section {
      border: 1px solid #dce2ee;
      border-radius: 20px;
      padding: 24px;
      background: white;
    }
    h2 {
      font-size: 1.1rem;
      margin-top: 0;
    }
    p {
      color: #465469;
      line-height: 1.6;
      overflow-wrap: anywhere;
    }
    [role='alert'] {
      color: #a12626;
    }
    button {
      min-height: 44px;
    }
  `,
})
export class ServiceConnection {
  readonly loading = signal(false);
  readonly message = signal('Comprueba la conexión antes de continuar.');
  readonly error = signal('');
  private readonly client = inject(PlatformClient);

  constructor() {
    this.check();
  }

  check(): void {
    if (this.loading()) return;
    this.loading.set(true);
    this.error.set('');
    this.client
      .getStatus()
      .pipe(
        timeout(10000),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({
        next: (status) => {
          this.message.set(
            `${status.product}: conexión establecida con el servicio ${status.apiVersion}.`,
          );
        },
        error: () => {
          this.error.set('No se pudo conectar con Kipo. Revisa tu conexión e inténtalo de nuevo.');
        },
      });
  }
}
