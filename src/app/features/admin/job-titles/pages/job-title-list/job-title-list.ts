import {Component, inject} from '@angular/core';
import {RouterLink} from '@angular/router';
import {AsyncPipe, DatePipe} from '@angular/common';
import {JobTitleService} from '../../services/job-title.service';
import {JobTitle} from '../../models/job-title.model';

@Component({
  selector: 'app-job-title-list',
  templateUrl: './job-title-list.html',
  imports: [RouterLink, AsyncPipe, DatePipe],
})
export class JobTitleList {
  private jobTitleService = inject(JobTitleService);
  jobTitles$ = this.jobTitleService.getAll();

  delete(jt: JobTitle) {
    if (confirm(`確定要刪除職稱「${jt.name}」嗎？`)) {
      this.jobTitleService.delete(jt.id).subscribe();
    }
  }
}
