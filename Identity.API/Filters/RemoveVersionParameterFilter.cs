﻿using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Identity.Filters
{
  public class RemoveVersionParameterFilter : IOperationFilter
  {
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
      var versionParameter = operation.Parameters.Single(p => p.Name == "version");
      operation.Parameters.Remove(versionParameter);
    }
  }
}
