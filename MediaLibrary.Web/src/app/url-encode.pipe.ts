import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
    name: 'urlEncode'
})
export class UrlEncodePipe implements PipeTransform {
    public transform(value: unknown): string {
        return encodeURIComponent('' + value);
    }
}
