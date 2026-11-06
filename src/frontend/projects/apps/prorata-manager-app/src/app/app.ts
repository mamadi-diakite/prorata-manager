import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';
import { ApiHealthService } from '@prorata-manager/shared/core';
import { Shell } from '@prorata-manager/shared/ui';

@Component({
  selector: 'pm-root',
  imports: [RouterOutlet, Shell],
  template: `
    <pm-shell appName="Prorata Manager" [apiStatus]="apiStatus()">
      <router-outlet />
    </pm-shell>
  `,
})
export class App {
  protected readonly apiStatus = toSignal(inject(ApiHealthService).check(), {
    initialValue: 'checking',
  });
}
