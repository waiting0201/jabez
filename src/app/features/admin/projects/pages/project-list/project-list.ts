import {Component, inject} from '@angular/core';
import {RouterLink} from '@angular/router';
import {AsyncPipe, DecimalPipe} from '@angular/common';
import {ProjectService} from '../../services/project.service';
import {Project} from '../../models/project.model';

@Component({
  selector: 'app-project-list',
  templateUrl: './project-list.html',
  imports: [RouterLink, AsyncPipe, DecimalPipe],
})
export class ProjectList {
  private projectService = inject(ProjectService);
  projects$ = this.projectService.getAll();

  delete(project: Project) {
    if (confirm(`確定要刪除專案「${project.code}」嗎？`)) {
      this.projectService.delete(project.id).subscribe();
    }
  }
}
