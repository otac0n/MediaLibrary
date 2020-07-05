import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
    name: 'fileName'
})
export class FileNamePipe implements PipeTransform {
    transform(value: string): string {
        value = value || '';
        return value.substr(Math.max(value.lastIndexOf('/'), value.lastIndexOf('\\')) + 1);
    }
}
