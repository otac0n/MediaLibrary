import { enableProdMode } from '@angular/core';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';

import { AppModule } from './app/app.module';

enableProdMode();

/* eslint-disable no-console */
platformBrowserDynamic().bootstrapModule(AppModule)
    .catch(err => console.error(err));
/* eslint-enable */
