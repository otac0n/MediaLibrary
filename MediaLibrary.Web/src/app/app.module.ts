import { HttpClientModule } from '@angular/common/http';
import { APP_INITIALIZER, NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { FileNamePipe } from './file-name.pipe';
import { HeartComponent } from './heart/heart.component';
import { PersonListComponent } from './person-list/person-list.component';
import { PersonComponent } from './person/person.component';
import { PreviewComponent } from './preview/preview.component';
import { staticDataInit } from './static-data';
import { TagListComponent } from './tag-list/tag-list.component';
import { TagComponent } from './tag/tag.component';
import { TagsService } from './tags.service';
import { UrlEncodePipe } from './url-encode.pipe';

@NgModule({
    declarations: [
        AppComponent,
        FileNamePipe,
        HeartComponent,
        PersonComponent,
        PersonListComponent,
        PreviewComponent,
        TagComponent,
        TagListComponent,
        UrlEncodePipe,
    ],
    imports: [
        AppRoutingModule,
        BrowserModule,
        FormsModule,
        HttpClientModule,
        NgbModule,
    ],
    providers: [
        { provide: APP_INITIALIZER, useFactory: staticDataInit, deps: [TagsService], multi: true },
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }
