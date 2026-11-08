import { ChangeDetectionStrategy, Component } from '@angular/core';
import { ServiceConnection } from 'shared-ui';

@Component({
  imports: [ServiceConnection],
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Home {}
